# AI Product Image Studio

Веб-додаток для створення товарів та автоматичної генерації їх зображень за допомогою ComfyUI та Stable Diffusion.

## Передумови

- ComfyUI (запущений на `http://localhost:8188`)
- .NET 8 SDK
- Node.js 18+
- Angular CLI (`npm install -g @angular/cli`)

## Швидкий старт

### 1. Запустити ComfyUI

```bash
cd ComfyUI_windows_portable
run_cpu.bat  # або run_nvidia_gpu.bat для NVIDIA GPU
```

Перевірте доступність: http://localhost:8188

### 2. Запустити .NET API

```bash
cd ShopProject/ShopAPI
dotnet restore
dotnet run
```

API доступний на: http://localhost:5000  
Swagger UI: http://localhost:5000/swagger

### 3. Запустити Angular Frontend

```bash
cd ShopProject/ShopFrontend
npm install
ng serve
```

Frontend доступний на: http://localhost:4200

## Використання

1. Відкрийте http://localhost:4200
2. Натисніть "+ Додати товар"
3. Заповніть форму: назва, колір, опис
4. Натисніть "Створити товар та згенерувати зображення"
5. Зачекайте 30-60 секунд поки згенерується зображення

## Структура проекту

```
ShopProject/
├── ShopAPI/              (.NET 8 Web API)
│   ├── Controllers/      # API контролери
│   ├── Services/         # Бізнес-логіка
│   ├── Models/           # Моделі даних
│   └── wwwroot/images/   # Згенеровані зображення
│
└── ShopFrontend/         (Angular 17)
    ├── src/app/
    │   ├── components/   # Компоненти UI
    │   ├── services/     # HTTP сервіси
    │   └── models/       # TypeScript моделі
```

## API Endpoints

### Товари
- `GET /api/products` - Список всіх товарів
- `GET /api/products/{id}` - Отримати товар за ID
- `POST /api/products` - Створити новий товар
- `PUT /api/products/{id}` - Оновити товар
- `DELETE /api/products/{id}` - Видалити товар

### Генерація зображень
- `POST /api/imagegeneration/generate` - Згенерувати зображення
- `POST /api/imagegeneration/generate-for-product/{id}` - Згенерувати для товару
- `GET /api/imagegeneration/status/{jobId}` - Перевірити статус генерації

## Налаштування

### Змінити модель в ComfyUI

Відредагуйте `ShopAPI/Services/ComfyUIService.cs`:

```csharp
["ckpt_name"] = "назва_вашої_моделі.safetensors"
```

### Змінити URL ComfyUI

Відредагуйте `ShopAPI/appsettings.json`:

```json
"ComfyUI": {
  "BaseUrl": "http://your-comfyui-url:8188"
}
```

## Troubleshooting

**ComfyUI не запускається**
- Перевірте наявність моделі в `ComfyUI/models/checkpoints/`
- Спробуйте запустити в CPU режимі: `run_cpu.bat`
- Перевірте логи в консолі

**API не може підключитися до ComfyUI**
- Переконайтесь, що ComfyUI запущений на порту 8188
- Перевірте налаштування в `appsettings.json`
- Перевірте firewall налаштування

**Зображення не генеруються**
- Перевірте логи в консолі .NET API
- Перевірте, чи працює ComfyUI
- Перевірте наявність моделі в ComfyUI
- Зачекайте 1-2 хвилини (генерація може тривати довше)

## Технології

- **Backend**: .NET 8, Entity Framework Core, HTTP Client
- **Frontend**: Angular 17, TypeScript, RxJS
- **AI**: ComfyUI, Stable Diffusion
- **Database**: In-Memory (для демо), можна замінити на SQL Server/PostgreSQL
