# Лабораторная работа №3 — Интеграционное тестирование (SQS + Localstack)

## Описание

В рамках третьей лабораторной работы:

- В оркестрацию (.NET Aspire AppHost) добавлено объектное хранилище (S3 в Localstack) и брокер сообщений (SQS в Localstack), создаваемые из CloudFormation-шаблона.
- Реализован файловый сервис `ProjectGenerator.FileService`, который читает сообщения из SQS, сериализует программные проекты в JSON и сохраняет их в бакет S3.
- В сервисе генерации `ProjectGenerator.Api` реализован продюсер, отправляющий сгенерированный программный проект в SQS-очередь.
- Реализованы интеграционные тесты

Вариант: **SQS + Localstack** (брокер — AWS SQS, объектное хранилище — S3 внутри Localstack).

## Сервисы

### ProjectGenerator.Api (продюсер)

- `IProjectProducer` / `SqsProjectProducer` — сериализует `SoftwareProject` в JSON и шлёт в SQS.
- `SoftwareProjectService.GetOrGenerate`: при кэш-промахе генерирует объект, отправляет в SQS, кладёт в Redis и возвращает клиенту.

### ProjectGenerator.FileService (консьюмер + S3)

- `SqsConsumerService : BackgroundService` — long-poll по SQS батчами до 10 сообщений; для каждого сообщения создаёт скоуп DI и вызывает `IS3Service.UploadFileAsync`.
- `IS3Service` / `S3Service` — кладёт файл в S3 под ключом `software-project_{id}.json`, поддерживает листинг и скачивание.
- `S3StorageController` — `GET /api/s3` (список ключей) и `GET /api/s3/{key}` (содержимое файла), Swagger UI в Development-режиме.

## API

| Метод | URL | Описание |
|---|---|---|
| GET | `/generate?id={id}` (через Gateway) | Возвращает сгенерированный программный проект; кладёт в Redis и шлёт в SQS |
| GET | `/api/s3` (FileService) | Список ключей файлов в S3 |
| GET | `/api/s3/{key}` (FileService) | Содержимое JSON-файла из S3 |

## Интеграционные тесты

Проект `ProjectGenerator.AppHost.Tests` (xUnit). Используется `IAsyncLifetime`: AppHost поднимается в `InitializeAsync`, останавливается в `DisposeAsync` — каждый тест работает с поднятым окружением.

## Предметная область — Программный проект

| № | Характеристика | Тип данных |
|---|---|---|
| 1 | Идентификатор в системе | int |
| 2 | Название проекта | string |
| 3 | Заказчик проекта | string |
| 4 | Менеджер проекта | string |
| 5 | Дата начала | DateOnly |
| 6 | Плановая дата завершения | DateOnly |
| 7 | Фактическая дата завершения | DateOnly? |
| 8 | Бюджет | decimal |
| 9 | Фактические затраты | decimal |
| 10 | Процент выполнения | int |

---

## Предыдущие лабораторные

### №2 — Балансировка нагрузки

API Gateway (Ocelot) с кастомным балансировщиком на основе параметра запроса (`индекс_реплики = id % количество_реплик`), 5 реплик `ProjectGenerator.Api` на портах 5000–5004.

### №1 — Кэширование

Сервис генерации программных проектов с кэшированием ответов через Redis (`IDistributedCache`), TTL — 15 минут.
