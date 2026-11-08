# Build és Futtatási Útmutató

## 1. Fejlesztői környezet és platform
* **Operációs rendszer:** Windows 11 (64-bit)
* **Fejlesztői környezet (IDE):** Microsoft Visual Studio 2022
* **Keretrendszer és nyelv:** .NET 9.0 (WPF), C#

## 2. Fordítási és Build folyamat

A forráskódból a futtatható bináris (`robot.exe`) előállítása az alábbi módszerekkel végezhető el:

### A) Visual Studio 2022 felületén
1. Nyisd meg a `.sln` fájlt Visual Studio 2022-ben.
2. A felső eszköztáron állítsd a build konfigurációt **Release** módra.
3. Válaszd a **Build > Build Solution** (vagy `Ctrl+Shift+B`) menüpontot.
4. A lefordított futtatható állomány (`robot.exe`) a `bin\Release\net9.0-windows\` mappában jön létre.

### B) Parancssorból (.NET CLI)
1. Nyiss egy parancssort (CMD) vagy PowerShell-t a `.csproj` fájlt tartalmazó mappában.
2. Futtasd az alábbi parancsot a projekt fordításához:
   ```cmd
   dotnet build -c Release
   ```
3. A generált `robot.exe` állomány a `bin\Release\net9.0-windows\` könyvtárban található.

## 3. Futtatás

Az alkalmazás a `bin\Release\net9.0-windows\` mappában lévő `robot.exe` fájlon keresztül indítható el parancssorból:

### Interaktív / GUI üzemmód (fejlesztői környezet és szimulátor elindítása)
```cmd
robot.exe input_001.txt output_001.txt
```

### Csendes (-q) üzemmód (automatikus tesztfuttatás felület nélkül)
```cmd
robot.exe -q input_001.txt output_001.txt
```