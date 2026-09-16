import { useEffect, useState } from 'react';
import * as signalR from '@microsoft/signalr';

export const useOrderHub = () => {
  const [connection, setConnection] = useState(null);
  const [orderStatus, setOrderStatus] = useState(null);

  useEffect(() => {
    const token = localStorage.getItem('token');
    const gatewayUrl = import.meta.env.VITE_GATEWAY_URL || 'http://localhost:5000';

    const hubConn = new signalR.HubConnectionBuilder()
      .withUrl(`${gatewayUrl}/orderhub`, {
        accessTokenFactory: () => token || '',
        skipNegotiation: false,
        transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling,
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000])
      .configureLogging(signalR.LogLevel.Information)
      .build();

    hubConn
      .start()
      .then(() => {
        console.log('SignalR OrderHub bağlantısı kuruldu.');

        hubConn.on('ReceiveOrderStatus', (data) => {
          console.log('Canlı Sipariş Güncellemesi:', data);
          setOrderStatus(data);
        });
      })
      .catch((err) => console.error('SignalR bağlantı hatası:', err));

    setConnection(hubConn);

    return () => {
      hubConn.stop();
    };
  }, []);

  return { connection, orderStatus };
};