const { app, BrowserWindow, shell, dialog } = require('electron');
const { autoUpdater } = require('electron-updater');
const path = require('node:path');

function createWindow() {
  const win = new BrowserWindow({
    width: 1440,
    height: 920,
    minWidth: 360,
    minHeight: 600,
    backgroundColor: '#07111f',
    autoHideMenuBar: true,
    webPreferences: {
      contextIsolation: true,
      nodeIntegration: false,
      sandbox: true
    }
  });
  win.loadFile(path.join(__dirname, 'www', 'index.html'));
  win.webContents.setWindowOpenHandler(({ url }) => {
    if (url.startsWith('https://') || url.startsWith('http://')) shell.openExternal(url);
    return { action: 'deny' };
  });
}
app.whenReady().then(() => {
  createWindow();
  // Packaged desktop builds check GitHub Releases for a newer signed installer.
  if (app.isPackaged) {
    autoUpdater.autoDownload = true;
    autoUpdater.autoInstallOnAppQuit = true;
    autoUpdater.on('update-downloaded', () => {
      dialog.showMessageBox({
        type: 'info',
        title: 'Обновление KARTI готово',
        message: 'Обновление KARTI скачано и будет установлено после закрытия приложения.',
        buttons: ['Понятно']
      });
    });
    autoUpdater.on('error', (error) => {
      console.error('KARTI auto-update failed:', error);
    });
    autoUpdater.checkForUpdatesAndNotify().catch(error => {
      console.error('KARTI update check failed:', error);
    });
    setInterval(() => {
      autoUpdater.checkForUpdatesAndNotify().catch(error => console.error('KARTI update check failed:', error));
    }, 6 * 60 * 60 * 1000);
  }
  app.on('activate', () => { if (BrowserWindow.getAllWindows().length === 0) createWindow(); });
});
app.on('window-all-closed', () => { if (process.platform !== 'darwin') app.quit(); });
