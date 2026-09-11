<div align="center">

<h1>🏋️ Kinect-alapú Mozgásosztályozó Rendszer</h1>

<p><strong>Valós idejű edzőtermi gyakorlatfelismerés gépi tanulással</strong></p>

[![Python](https://img.shields.io/badge/Python-3.7-3776AB?style=flat-square&logo=python&logoColor=white)](https://python.org)
[![TensorFlow](https://img.shields.io/badge/TensorFlow-Keras-FF6F00?style=flat-square&logo=tensorflow&logoColor=white)](https://tensorflow.org)
[![scikit-learn](https://img.shields.io/badge/scikit--learn-ML-F7931E?style=flat-square&logo=scikitlearn&logoColor=white)](https://scikit-learn.org)
[![OpenPose](https://img.shields.io/badge/OpenPose-CMU-blueviolet?style=flat-square)](https://github.com/CMU-Perceptual-Computing-Lab/openpose)
[![Platform](https://img.shields.io/badge/platform-Windows-0078D6?style=flat-square&logo=windows&logoColor=white)]()
[![Status](https://img.shields.io/badge/státusz-kész-brightgreen?style=flat-square)]()

<p>
  <a href="#a-rendszerről">A rendszerről</a> •
  <a href="#eredmények">Eredmények</a> •
  <a href="#adatpipeline">Adatpipeline</a> •
  <a href="#kulcsdöntések">Kulcsdöntések</a> •
  <a href="#technológiák">Technológiák</a> •
  <a href="#futtathatóság">Futtathatóság</a>
</p>

</div>

---

## A rendszerről

A projekt egy **végponttól végpontig épített mozgásosztályozó rendszer**, amely Microsoft Kinect szenzorral rögzíti a felhasználó mozgását, OpenPose-zal azonosítja a csontváz kulcspontjait, majd LSTM neurális hálózattal valós időben felismeri az elvégzett edzőtermi gyakorlatot. A felismerés mellett a rendszer alapszintű mozgáselemzést is végez – ez azonban másodlagos funkció, a projekt fókusza a mozgásosztályozás pontosságán és az ML-modellek összehasonlításán van.

A rendszer tervezése során az volt a kérdés: lehetséges-e versenyképes pontosságot elérni fogyasztói szintű szenzorral, a kapcsolódó irodalomban szereplő megközelítésekkel összehasonlítva?

---

## Eredmények

Négy ML-modellt hasonlítottam össze azonos adatkészleten, precision / recall / F1 metrikákkal:

| Modell | Teszt pontosság | Megjegyzés |
|---|:---:|---|
| **LSTM** ✅ | **~93%** | Időbeli sorozatokhoz ideális – végső modell |
| 2D CNN | 66% | Kis adathalmazon erősen túltanul |
| SVM | **~94%**|  |
| Random Forest | 88% | Robusztus, de statikus jellemzőkre optimalizált |

**Kontextus az irodalomban:**

| Tanulmány | Módszer | Pontosság |
|---|---|:---:|
| Sejera et al. | Kinect V2 + SVM | 90.54% |
| Hussain et al. | LSTM + viselhetőszenzor | 82% |
| **Ez a projekt** | **Kinect V1 + LSTM** | **~93%** |

> A rendszer a Kinect v1 korlátaival (kisebb pontosság az alsó testnél, zajosabb mélységi adat) együtt is versenyképes eredményt ért el.

---

## Adatpipeline

```
Kinect V1 szenzor (30 FPS, RGB)
        │
        ▼  [minden 4. képkocka kerül feldolgozásra]
 ┌──────────────────┐
 │  OpenPose szerver │  ← Python, TCP :1111
 │  25 kulcspont     │
 └────────┬─────────┘
          │  JSON: [x, y] koordináták képkockánként
          ▼
 ┌──────────────────────────┐
 │  DataAnalyzer  (C#)      │
 │  Δ elmozdulási vektorok  │  ← pozíciófüggetlen jellemzők
 │  normalizálás            │
 │  25 képkockás ablak      │  ← ~1 ismétlés (~3 másodperc)
 └────────┬─────────────────┘
          │
          ▼  JSON vektorsorozat
 ┌──────────────────────┐
 │  LSTM osztályozó     │  ← Python, TCP :2222
 │  → felismert gyakorlat│
 └──────────────────────┘
          │
          ▼
   Eredmény megjelenítése a WPF felületen
```

---

## Kulcsdöntések

> Ez a rész mutatja meg az analitikus gondolkodást a projekt mögött.

**🔑 Elmozdulási vektorok elemzsése a nyers koordináták helyett**
A nyers testtartási koordináták pozíciófüggők – ugyanaz a guggolás más értékeket ad, ha a felhasználó közelebb vagy távolabb áll a szenzortól. Képkockák közötti elmozdulási vektorok számításával a rendszer **függetlenné vált a pozíciótól** . Ez volt az egyetlen átalakítás, amely szignifikánsan javította az összes modell pontosságát.

**🔑 Mélységcsatorna kizárása**
A Kinect v1 mélységi adatai 1,8 méteren túl >40 mm hibával rendelkeznek, az alsó test ízületeire különösen pontatlan. Az RGB-only pipeline kiszámíthatóbb és stabilabb eredményt adott, mint a zajos mélységi adat kombinálása.

**🔑 K-Means az irányfelismeréshez**
Az osztályozók ugyanolyan vektorsorozatot látnak bal és jobb oldali állásból, de tükrözve. Ahelyett, hogy minden gyakorlatból két verziót tanítottam volna, egy külön K-Means modult alkalmaztam az irány előzetes meghatározásához – 96%-os pontossággal.

---

## Technológiák

| Terület | Eszköz |
|---|---|
| Mélytanulás | TensorFlow + Keras (LSTM, 2D CNN) |
| Klasszikus ML | scikit-learn (SVM, Random Forest, K-Means) |
| Adatfeldolgozás | Pandas, NumPy, OpenCV |
| Pózdetektálás | OpenPose (CMU), Kinect SDK 1.8 |
| Architektúra | C# WPF (.NET 4.8), TCP socket szerver |
| Értékelés | Precision, Recall, F1 score, Confusion matrix |

---

## Repository felépítése

```
kinect-exercise-recognition/
├── realtime/
│   ├── DepthBasics-WPF/          # C# WPF kliens: Kinect stream, képkockakezelés, vektorképzés, gyakorlatelemzők
│   └── python/
│       ├── server.py             # OpenPose kulcspont-szerver (TCP :1111), az osztályozót is elindítja
│       └── classifier_server.py  # LSTM osztályozó (TCP :2222)
├── models/lstm/                  # A futó rendszer által betöltött LSTM (TensorFlow SavedModel)
├── notebooks/
│   ├── lstm.ipynb                # LSTM tanítás és kiértékelés
│   ├── random_forest.ipynb
│   └── cnn_2d.ipynb
├── data/                         # Megmaradt kulcspont-adatok, leírás: data/README.md
├── requirements.txt
└── README.md
```

> A szakdolgozat eredeti, változatlan repója: [bturnai/ProjektMunka](https://github.com/bturnai/ProjektMunka)

---


## Futtathatóság

> ⚠️ **A teljes rendszer futtatásához két speciális feltétel szükséges:**
> - **Microsoft Kinect v1 szenzor** (2010, Xbox periféria) – ma már nehezen hozzáférhető
> - **Kompatibilis processzor** – egyes könyvtárak (pl. OpenPose) csak adott CPU/GPU paraméterek esetén futnak megfelelően
>
> **A modellek tanítása és kiértékelése a `notebooks/` mappában, a kimenetekkel együtt ezek nélkül is áttekinthető.**

```bash
# Python függőségek (a szakdolgozati környezet: Python 3.7)
pip install -r requirements.txt

# OpenPose telepítése (külön szükséges):
# https://github.com/CMU-Perceptual-Computing-Lab/openpose
```

***

## Továbbfejlesztési lehetőségek

- [ ] **BlazePose** – OpenPose helyett, GPU nélkül is fut, mobilra is portolható
- [ ] **Mobilos verzió** – TensorFlow Lite + okostelefon kamera, Kinect nélkül
- [ ] **Edzésnapló dashboard** – Pandas + Matplotlib alapú haladáskövetés

---

<div align="center">

**Turnai Bálint Ádám** · Óbudai Egyetem NIK · 2025

[![GitHub](https://img.shields.io/badge/GitHub-bturnai-181717?style=flat-square&logo=github)](https://github.com/bturnai)
[![LinkedIn](https://img.shields.io/badge/LinkedIn-Turnai_Bálint-0A66C2?style=flat-square&logo=linkedin)](https://linkedin.com/in/turnai-balint-adam)

*Szakdolgozat · NIK-CMJ194 · 2024/25 tanév*

</div>
