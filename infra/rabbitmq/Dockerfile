# syntax=docker/dockerfile:1.7
# docker-compose'daki önceki image'ın aynısı + Order saga timeout'u (MassTransit UseDelayedMessageScheduler) için
# rabbitmq_delayed_message_exchange plugin'i.
FROM rabbitmq:3.13-management-alpine

# Plugin sürümü RabbitMQ minor sürümüyle eşleşmelidir (3.13.x -> plugin 3.13.x). Base image güncellenirse burası da kontrol edilmeli.
ARG DELAYED_EXCHANGE_VERSION=3.13.0
ARG DELAYED_EXCHANGE_SHA256=3479180df03fa830fd59ccf638a60eee8f0e8261524a8616685522d7b8d9a9ee

ADD --checksum=sha256:${DELAYED_EXCHANGE_SHA256} --chmod=644 \
    https://github.com/rabbitmq/rabbitmq-delayed-message-exchange/releases/download/v${DELAYED_EXCHANGE_VERSION}/rabbitmq_delayed_message_exchange-${DELAYED_EXCHANGE_VERSION}.ez \
    /opt/rabbitmq/plugins/

# --offline: broker çalışmadan /etc/rabbitmq/enabled_plugins dosyasına yazar (volume /var/lib/rabbitmq'yu etkilemez)
RUN rabbitmq-plugins enable --offline rabbitmq_delayed_message_exchange
