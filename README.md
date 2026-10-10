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


## Automatic updates

- **Windows:** packaged Electron builds check GitHub Releases at launch and every six hours. Downloaded updates install after the app closes. A release must contain the installer and electron-builder update metadata; a branch build alone is not an update release.
- **Website/PWA:** the service worker checks for a new app shell on launch, when the tab becomes visible, after reconnecting, and hourly. When a new worker is ready, KARTI offers an Update button and reloads after activation.
- **Android APK:** KARTI checks the latest GitHub Release and shows an update link only when that release includes an APK asset. Android validates the APK signing certificate before allowing an in-place update; it cannot safely self-install silently.

### Android release signing (required for in-place updates)

The workflow's debug APK is for testing only and is not a stable update channel. To publish an updateable APK, configure these repository Actions secrets with a private keystore that you keep safe and reuse for every release:

- `KARTI_ANDROID_KEYSTORE_BASE64`: Base64-encoded contents of the release keystore.
- `KARTI_ANDROID_STORE_PASSWORD`: keystore password.
- `KARTI_ANDROID_KEY_ALIAS`: key alias.
- `KARTI_ANDROID_KEY_PASSWORD`: key password.

Never commit the keystore or passwords to the repository. The workflow builds and attaches a signed APK to a tagged GitHub Release only when all four secrets are present. The first public Android installation must use that same signing key; an APK signed with a different key cannot update it in place. Existing debug APK installations may need to be uninstalled before switching to a release-signed build.

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
