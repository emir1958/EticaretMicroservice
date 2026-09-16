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

    // 🟢 1. DÜZELTME: Backend'in gönderdiği doğru event adı "ReceiveOrderState"
    connection.on("ReceiveOrderState", (status) => {
      if (isMounted) {
        setOrderStatus(status);
      }
    });

    const startConnection = async () => {
      try {
        await connection.start();
        if (isMounted) {
          console.log("🟢 SignalR OrderHub bağlandı.");
          
          // 🟢 2. DÜZELTME: Backend'deki gruba katılma metodunu çağır!
          await connection.invoke("JoinOrderGroup");
          console.log("🟢 JoinOrderGroup başarılı, kullanıcı grubuna katılındı.");
        }
      } catch (err) {
        if (err.name !== "AbortError" && !err.message?.includes("negotiation")) {
          console.error("SignalR bağlantı/invoke hatası:", err);
        }
      }
    };

    // Yeniden bağlanma (reconnect) durumunda gruba tekrar katıl
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