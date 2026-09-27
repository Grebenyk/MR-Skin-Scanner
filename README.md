# MR Skin Scanner

Mixed-reality скан шкіри для Meta Quest 3 з віддаленим AI-сервісом. У passthrough
користувач наводить рамку-scope на ділянку шкіри, натискає тригер — застосунок
захоплює зображення реальної камери, надсилає на сервер і показує ймовірний стан
шкіри на панелі перед собою.

> Навчальний / допоміжний демонстратор — НЕ медичний виріб. Результат не є діагнозом;
> для точної оцінки зверніться до дерматолога.

## Структура
- `server/` — Django + PyTorch AI-сервіс (ендпоінт `POST /api/predict/`). Класифікатор
  MobileNet V2, 6 класів: acne, atopic dermatitis, eczema, healthy, melanoma, psoriasis.
  Деталі — `server/DEPLOY.md`.
- `client/` — Unity 6 MR-застосунок для Meta Quest 3 (Meta XR All-in-One SDK,
  Passthrough Camera API). Деталі — `client/PHASE2_SETUP.md`, `client/PHASE2_NETWORKING.md`.

## Запуск сервера

cd server
python3.12 -m venv .venv && source .venv/bin/activate
pip install -r requirements.txt
export DJANGO_DEBUG=true DJANGO_ALLOWED_HOSTS="*"
python manage.py migrate
python manage.py runserver 0.0.0.0:8000
Публічний доступ для шолома: `cloudflared tunnel --url http://localhost:8000`.

## Запуск клієнта
Відкрити `client/` в Unity 6000.4.x, вписати URL тунелю в компонент **Skin Api Client**,
Build & Run на Meta Quest 3 (Horizon OS v74+).

## Команда
Черкаський державний технологічний університет — Pavlo Grebenyuk (lead), Andrii Vizitiu,
Maksym Shypilkin, Daria Skakun. Керівник: Sergii Holub.
