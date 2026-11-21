import { useEffect, useState } from "react";
import * as signalR from "@microsoft/signalr";
import { env } from "../../config/env";

export interface OrderStateMessage {
  orderId: number;
  status: "Completed" | "Canceled";
  message: string;
}

// Bileşen bağlantı kurulurken unmount olursa (StrictMode, sayfa değişimi) start() bu hatalarla iptal edilir
function isExpectedStopError(err: unknown) {
  return err instanceof Error && (err.name === "AbortError" || err.message.includes("negotiation"));
}

export function useOrderHub() {
  const [orderStatus, setOrderStatus] = useState<OrderStateMessage | null>(null);

  useEffect(() => {
    let isMounted = true;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(env.orderHubUrl, {
        accessTokenFactory: () => localStorage.getItem("token") ?? "",
      })
      .withAutomaticReconnect()
      // Kütüphanenin kendi logları kapalı: bağlantı hataları aşağıda start() içinde ele alınır.
      // Aksi halde StrictMode'un açılırken kapattığı bağlantı bile konsola hata olarak düşer.
      .configureLogging(signalR.LogLevel.None)
      .build();

    connection.on("ReceiveOrderState", (message: OrderStateMessage) => {
      if (isMounted) setOrderStatus(message);
    });

    connection.onreconnected(() => {
      connection.invoke("JoinOrderGroup").catch((err: unknown) => {
        console.error("Yeniden bağlandıktan sonra gruba katılınamadı:", err);
      });
    });

    const start = async () => {
      try {
        await connection.start();
        if (isMounted) await connection.invoke("JoinOrderGroup");
      } catch (err) {
        if (!isExpectedStopError(err)) console.error("SignalR bağlantı hatası:", err);
      }
    };

    start();

    return () => {
      isMounted = false;
      connection.off("ReceiveOrderState");
      connection.stop().catch(() => {});
    };
  }, []);

  return { orderStatus };
}
