# KARTI 🗺️

**KARTI** — первая версия интерактивного картографического приложения. Идея: простой и красивый способ находить места и исследовать мир.

## Что работает в MVP

- Интерактивная карта с перемещением и масштабированием.
- Поиск городов, адресов и мест.
- Геолокация устройства (после разрешения пользователя).
- Построение автомобильного маршрута между двумя точками.
- Выбор точек нажатием на карту.
- Адаптивный интерфейс для телефона и компьютера.

## Как запустить

Проект — статический сайт, сборка не нужна. Открой `index.html` в браузере или включи GitHub Pages:

1. Открой **Settings → Pages** в репозитории.
2. В разделе **Build and deployment** выбери **Deploy from a branch**.
3. Выбери ветку **main** и папку **/(root)**, затем нажми **Save**.

После публикации GitHub покажет адрес сайта в настройках Pages.

## Используемые сервисы

- [Leaflet](https://leafletjs.com/) — интерактивная карта.
- [OpenStreetMap](https://www.openstreetmap.org/copyright) — карта и данные мест.
- [Nominatim](https://nominatim.org/) — поиск адресов и мест.
- [OSRM](https://project-osrm.org/) — автомобильные маршруты.

Это ранний прототип: поиск и маршруты требуют интернета и зависят от доступности публичных сервисов. Для реального запуска на большую аудиторию нужно настроить собственные или коммерческие геосервисы с учётом их лимитов и условий использования.

## Установка KARTI на устройства

KARTI поддерживает формат Progressive Web App (PWA): после публикации по HTTPS приложение можно установить из совместимого браузера, не меняя текущую веб-версию.

- **Android (Chrome и совместимые браузеры):** открой сайт KARTI и выбери «Установить приложение» или «Добавить на главный экран».
- **iPhone/iPad (Safari):** открой сайт, нажми «Поделиться» и выбери «На экран Домой».
- **Windows/macOS (совместимые браузеры):** используй значок установки в адресной строке или меню браузера.
- **Браузер:** сайт продолжает работать как обычное веб-приложение.

В репозитории добавлены `manifest.webmanifest`, `icon.svg` и `sw.js`. Service worker кеширует оболочку приложения для повторного открытия. **Это не полноценный офлайн-режим карты:** картографические тайлы, поиск и построение маршрутов по-прежнему зависят от интернета и внешних сервисов.

### Публикация

Включи GitHub Pages в **Settings → Pages → Deploy from a branch**, выбрав ветку `main` и папку `/(root)`. Установка PWA требует HTTPS (GitHub Pages предоставляет HTTPS для опубликованного сайта).


## Build Android APK and Windows EXE

The GitHub Actions workflow `.github/workflows/build-installers.yml` builds installable packages from the `pwa-install-ready` branch.

1. Open the repository's **Actions** tab.
2. Select **Build KARTI installers**.
3. Click **Run workflow** and choose `pwa-install-ready`.
4. When both jobs finish, open the workflow run and download the artifacts:
   - `KARTI-Android-APK` contains `app-debug.apk`.
   - `KARTI-Windows-EXE` contains the Windows setup installer.
5. On Android, transfer `app-debug.apk` to the phone and open it. Android may ask you to allow installing apps from that file manager/browser; only proceed if you trust the file you built.
6. On Windows, extract the downloaded artifact ZIP and run `KARTI-Setup-1.0.0-x64.exe`. Windows SmartScreen may warn because this initial build is unsigned.

These are test builds, not store-ready signed releases. Maps, place search, and route planning still need an internet connection and public services may be blocked or rate-limited in some networks. The APK/EXE workflow itself must complete successfully before downloadable installers exist.
