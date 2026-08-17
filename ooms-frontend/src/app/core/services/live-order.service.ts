import { DestroyRef, Injectable, inject, signal } from '@angular/core';
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr';
import { environment } from '../../../environments/environment';
import { LiveOrder } from '../models/order.models';
import { AuthService } from './auth.service';

export type LiveConnectionState = 'disconnected' | 'connecting' | 'connected' | 'reconnecting';

/**
 * Real-time order feed over the `/hubs/orderHub` SignalR hub.
 *
 * The hub itself is registered in Program.cs and exposes `JoinBranchGroup`,
 * which puts the connection into a `branch-{id}` group. The server-side
 * broadcast in OrderController.PlaceOrder is currently COMMENTED OUT:
 *
 *     // await _hubContext.Clients.Group($"branch-{dto.BranchId}")
 *     //     .SendAsync("ReceiveOrder", orderDtO);
 *
 * So until that is re-enabled no `ReceiveOrder` events arrive and the admin
 * board falls back to polling. This client is written to light up the instant
 * the server starts publishing - see BACKEND_SETUP.md for the one-line change.
 */
@Injectable({ providedIn: 'root' })
export class LiveOrderService {
  private readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);

  private connection: HubConnection | null = null;
  private joinedBranches = new Set<number>();

  /** Connection lifecycle, for the "live"/"offline" pill in the UI. */
  readonly state = signal<LiveConnectionState>('disconnected');
  /** The most recent order pushed by the server, or null. */
  readonly lastOrder = signal<LiveOrder | null>(null);
  /** Incremented on every push so consumers can react with a single effect. */
  readonly revision = signal(0);

  constructor() {
    this.destroyRef.onDestroy(() => void this.stop());
  }

  /**
   * Opens the hub connection (idempotent) and joins the given branch groups.
   * Safe to call repeatedly - joining a group twice is a no-op server-side.
   */
  async start(branchIds: number[] = []): Promise<void> {
    if (this.connection?.state === HubConnectionState.Connected) {
      await this.joinBranches(branchIds);
      return;
    }
    if (this.state() === 'connecting') return;

    this.state.set('connecting');

    const connection = new HubConnectionBuilder()
      .withUrl(`${environment.apiBaseUrl}${environment.orderHubUrl}`, {
        // The hub does not require auth today, but sending the token keeps
        // this working if [Authorize] is added to OrderHub later.
        accessTokenFactory: () => this.auth.token() ?? '',
      })
      .withAutomaticReconnect([0, 2_000, 5_000, 10_000, 30_000])
      .configureLogging(environment.production ? LogLevel.Error : LogLevel.Warning)
      .build();

    connection.on('ReceiveOrder', (order: LiveOrder) => {
      this.lastOrder.set(order);
      this.revision.update((n) => n + 1);
    });

    // Emitted by OrderHub.JoinBranchGroup back to the caller.
    connection.on('BranchRegistered', () => {
      /* acknowledgement only */
    });

    connection.onreconnecting(() => this.state.set('reconnecting'));
    connection.onreconnected(async () => {
      this.state.set('connected');
      // Group membership is lost across a reconnect - re-join.
      const branches = [...this.joinedBranches];
      this.joinedBranches.clear();
      await this.joinBranches(branches);
    });
    connection.onclose(() => this.state.set('disconnected'));

    this.connection = connection;

    try {
      await connection.start();
      this.state.set('connected');
      await this.joinBranches(branchIds);
    } catch {
      // No backend, or the hub is unreachable. Callers keep polling.
      this.state.set('disconnected');
      this.connection = null;
    }
  }

  private async joinBranches(branchIds: number[]): Promise<void> {
    if (this.connection?.state !== HubConnectionState.Connected) return;
    for (const id of branchIds) {
      if (this.joinedBranches.has(id)) continue;
      try {
        await this.connection.invoke('JoinBranchGroup', String(id));
        this.joinedBranches.add(id);
      } catch {
        /* group join failed - polling still covers us */
      }
    }
  }

  async stop(): Promise<void> {
    this.joinedBranches.clear();
    const connection = this.connection;
    this.connection = null;
    this.state.set('disconnected');
    if (connection) {
      try {
        await connection.stop();
      } catch {
        /* already closed */
      }
    }
  }
}
