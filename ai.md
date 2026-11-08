# ai.md – Mesterséges intelligencia használata

# 1\. Használt AI-eszközök és modellek

* **Papp Dániel Dominik (értelmező):** Claude Sonnet 5.5 (Anthropic), claude.ai chatfelület
* **Szilágyi Zsombor (robotszimulátor):** Claude Sonnet 5.5 (Anthropic), claude.ai chatfelület
* **Szilágyi Bálint (blokkszerkesztő):** Claude Sonnet 4.6 (Anthropic), claude.ai chatfelület

## 2\. Mire használtuk, és mekkora része készült AI segítségével

* **Papp Dániel Dominik:** az AI a tesztelésben és a függvények megtervezésében segített. Az AI által írt rész aránya: \[kb. 30%].
* **Szilágyi Zsombor:** az AI a tesztelésben és a függvények megtervezésében segített, a két legnehezebb függvény (`StepForward()`, `OnRender()`) első változatát pedig ő írta. Az AI által írt rész aránya: \[kb. 50%].
* **Szilágyi Bálint:** az AI a tesztelésben és a függvények megtervezésében segített. Az AI által írt rész aránya: \[kb. 50%].

## 3\. Hogyan ellenőriztük és javítottuk az eredményt

* A kódot a feladatkiírással vetettük össze (irányok, hibakódok, naplóformátum, felfedezett mezők szabálya).
* Az AI-t a már kész részekhez igazítottuk: a szimulátor kódja előtt elkértük az `IRobotController` és az `Interpreter.cs` tartalmát, és nem találgattunk metódusneveket.

## 4\. Kiemelt függvények

### Papp Dániel Dominik: `FinishIfDone()` (`Interpreter.cs`)

**Mit csinál:** minden sor után eldönti, véget ért-e a futás. Ha elfogytak a sorok, lezárja a naplót `EOR`-ral. Ha az 1000. feldolgozott sor után még jönne egy sor, az `INSTRUCTION\_COUNT\_LIMIT\_EXCEEDED` kerül a naplóba, `EOR` nélkül. Közben némán átugorja azokat az `else` ágakat, amelyekbe az `if` igaz ága után "estünk bele".

**Miért volt nehéz:** az `else` sora csak akkor kerülhet a naplóba, ha az ág ténylegesen lefutott, a limit esetén pedig pontosan az 1001. naplósor a hibajelzés, és a két záró eset (`EOR` és limit) kizárja egymást.

### Szilágyi Zsombor: `StepForward()` (a `Front()` és `At()` segédfüggvényekkel)

**Mit csinál:** egy mezőt léptet a robot előtti irányba. Ha a következő mező fal vagy a térképen kívül van, `E\_BEDROCK\_HIT` hibát dob, és a robot a fal előtt marad. A lépéssel a mező felfedezetté válik.

**Miért volt nehéz:** a térképen kívüli mező a feladat szerint falnak számít, ezért az `At(r, c)` minden indexelés előtt ellenőrzi a határokat. A szöveg nem mondja ki egyértelműen, hogy a `step forward n` falba ütközéskor a robot a fal elé lép-e, vagy a helyén marad. Az értelmező `n`-szer hívja a `StepForward()`-ot, így a robot a fal elé lép és ott áll meg. A hibát az értelmező csak `MineScriptException` formában kapja el, ezért a robot is ezt dobja.

### Szilágyi Zsombor: `OnRender()` (`Engine.xaml.cs`)

**Mit csinál:** méretezi és középre igazítja a térképet, ködként rajzolja a nem felfedezett mezőket, az aranyat csak a felfedezett mezőn mutatja, a robotot pedig a nézési irányába forduló háromszögként rajzolja, előtte a szenzorcsíkkal.

**Miért volt nehéz:** a sorok lefelé nőnek, ezért az észak a `-1` sor, és az irányvektorból, valamint a rá merőleges vektorból kell kiszámolni a háromszög csúcsait mind a négy irányban. A szenzor a feladat szerint nem tesz felfedezetté mezőt, ezért a csík színe a `Front()`-ból jön, a ködöt viszont csak a `seen` tömb vezérli.

### Szilágyi Bálint: `BuildUI()` (`MainWindow.xaml.cs`)

**Mit csinál:** rekurzívan felépíti egy blokk és a törzsében lévő gyerekblokkok megjelenítését, kezeli az ejtést (drag and drop), a törlést és a beágyazási mélység korlátját, majd minden változás után újrarajzol és frissíti a forráskódot.

**Miért volt nehéz:** a blokkfa és a felület állapotát szinkronban kell tartani, a beágyazás legfeljebb három szint lehet, és a szerkesztésnek bármilyen sorrendben működnie kell.

## 5\. Öt konkrét eset

1. **Hibatípus egyeztetése (sikeres).** Az AI első szimulátorkódja `ArgumentException`-t dobott, de az új értelmező csak `MineScriptException`-t kapja el. Ezt az értelmező forráskódjának átnézésekor vettük észre, még mielőtt futásidejű hibát okozott volna.
2. **Ne találgasson az AI (sikeres).** A `TurnBy(int fok)` névből nem derült ki, hogy fok vagy negyedfordulat. Az AI kódolás helyett elkérte az értelmezőt, és abból látszott, hogy `n % 4` negyedfordulatot ad át.
3. **AI mint kódellenőr (sikeres).** Az értelmező első változatában az AI kiszúrta, hogy a `step forward` nem veszi figyelembe az `n` értékét, a `print` nincs kezelve, és az `Execute` blokkolja a felületet. Ezek alapján újraírtuk.
4. **Build-hiba téves diagnózisa (részben sikertelen).** A `Status` mező hiányára az AI először XAML-gyökérelem- és névtérproblémát feltételezett, de a valódi ok egy `git pull` utáni elavult `obj` mappa volt, amit a *Rebuild Solution* megoldott.
5. **Színek becslése képről (korlátozott).** Az AI a sötét téma színeit az IDE képernyőképéből becsülte meg, ezért a pontos színekhez a felületért felelős tagtól kellett értéket kérni.

