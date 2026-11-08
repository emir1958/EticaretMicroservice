import { useEffect, useState, useRef } from "react";
import * as signalR from "@microsoft/signalr";

export function useOrderHub() {
  const [orderStatus, setOrderStatus] = useState(null);
  const connectionRef = useRef(null);

  useEffect(() => {
    let isMounted = true;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl("http://localhost:5000/orderhub", {
        accessTokenFactory: () => localStorage.getItem("token") || "",
      })
      .withAutomaticReconnect()
      .build();

    connectionRef.current = connection;

    // 🟢 DÜZELTME: Backend'den ister OrderId ister orderId gelsin, normalize ediyoruz
    connection.on("ReceiveOrderState", (data) => {
      if (isMounted && data) {
        setOrderStatus({
          orderId: data.orderId ?? data.OrderId,
          status: data.status ?? data.Status,
          message: data.message ?? data.Message,
        });
      }
    });

    const startConnection = async () => {
      try {
        await connection.start();
        if (isMounted) {
          console.log("🟢 SignalR OrderHub bağlandı.");
          await connection.invoke("JoinOrderGroup");
          console.log("🟢 JoinOrderGroup başarılı, kullanıcı grubuna katılındı.");
        }
      } catch (err) {
        if (err.name !== "AbortError" && !err.message?.includes("negotiation")) {
          console.error("SignalR bağlantı/invoke hatası:", err);
        }
      }
    };

    connection.onreconnected(async () => {
      try {
        await connection.invoke("JoinOrderGroup");
        console.log("🔄 SignalR yeniden bağlandı, gruba tekrar katılındı.");
      } catch (e) {
        console.error("Yeniden bağlanma sonrası gruba katılma hatası:", e);
      }
    });

    startConnection();

    return () => {
      isMounted = false;
      if (connection) {
        connection.off("ReceiveOrderState");
        if (connection.state === signalR.HubConnectionState.Connected) {
          connection.stop().catch(() => {});
        }
      }
    };
  }, []);

  return { orderStatus };
}