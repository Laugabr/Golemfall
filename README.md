# Golemfall
 
![Unity](https://img.shields.io/badge/Unity-6-000000?logo=unity)
![Photon Fusion](https://img.shields.io/badge/Photon%20Fusion-2-blue)
![C#](https://img.shields.io/badge/C%23-.NET-239120?logo=csharp)
![Status](https://img.shields.io/badge/status-in%20development-yellow)
 
**ES —** Action-RPG cooperativo 3D en red: explorá, crafteá, subí de nivel y peleá contra el boss de la arena con tus amigos.
**EN —** 3D co-op action-RPG with online multiplayer: explore, craft, level up and take down the arena boss with your friends.
 

<img width="430" height="243" alt="ss4" src="https://github.com/user-attachments/assets/17705358-f1e9-4107-aaed-4eff34e8abe8" />
<img width="452" height="254" alt="ss3" src="https://github.com/user-attachments/assets/4cb1bd77-fd89-4fec-8604-85eb2c2cca5e" />

---
 
## 🎮 Sistemas / Systems
 
| Español | English |
|---|---|
| Multijugador host-autoritativo con Photon Fusion 2 | Host-authoritative multiplayer with Photon Fusion 2 |
| Movimiento con SimpleKCC: salto, dash con air-dashes limitados y rotación predicha | SimpleKCC movement: jump, capped air-dashes, client-predicted rotation |
| Combate por habilidades (melee, a distancia, curación) con cooldowns en red | Ability-driven combat (melee, ranged, heal) with networked cooldowns |
| Inventario y equipamiento replicados, con drag & drop y quick-move | Replicated inventory & equipment with drag-and-drop and quick-move |
| Crafteo de 2 ítems con recetas en ScriptableObjects, validado en el servidor | Two-item crafting from ScriptableObject recipes, server-validated |
| Progresión: curva de XP, niveles, bonus de stats y desbloqueo de habilidades | Progression: XP curve, levels, stat bonuses and ability unlocks |
| Misiones y tracking de eventos de gameplay | Missions and gameplay event tracking |
| Arena de boss con respawn grupal ante wipe del equipo | Boss arena with group respawn on team wipe |
| Destructibles, drops, VFX en red, HUD, notificaciones y cloud save | Destructibles, drops, networked VFX, HUD, notifications and cloud save |
 
## ⌨️ Controles / Controls
 
| Input | Acción / Action |
|---|---|
| `WASD` | Movimiento / Move |
| `Space` | Salto / Jump |
| `Shift` | Dash |
| `F` | Interactuar, recoger ítem / Interact, pick up |
| `Click izq. / LMB` | Ataque melee / Melee attack |
| `Q` | Ataque a distancia / Ranged attack |
| `E` | Curación / Heal |
| `Click der. / RMB` | Cámara / Camera |
| `I` | Inventario / Inventory |
| `Esc` | Pausa, cerrar paneles / Pause, close panels |
 
## 🧠 Decisiones técnicas / Technical highlights
 
- **Autoridad en el servidor / Server authority** — El host resuelve daño, crafteo, XP y spawns; los clientes solo proponen inputs y RPCs. *The host resolves damage, crafting, XP and spawns; clients only propose inputs and RPCs.*
- **Input desacoplado del tick / Tick-independent input** — El input se acumula en `BeforeUpdate` y se entrega en `OnInput`, con un latch para el clic izquierdo: un click rápido entre dos ticks de Fusion no se pierde. *Input accumulates in `BeforeUpdate` and is delivered in `OnInput`, with a latch so fast clicks between ticks are never dropped.*
- **Rollback-safe** — `PreviousButtons` es `[Networked]`, así la detección de flancos (`WasPressed`) sigue siendo correcta durante resimulaciones. *`PreviousButtons` is `[Networked]`, keeping edge detection correct through resimulations.*
- **Predicción local / Client-side prediction** — Rotación y dash se aplican de inmediato en el cliente y Fusion reconcilia con el snapshot del host: control instantáneo sin sacrificar autoridad. *Rotation and dash apply instantly on the client and Fusion reconciles against the host snapshot: instant feel, no loss of authority.*
- **VFX desacoplados del emisor / Emitter-independent VFX** — Un `NetworkVFXManager` permanente dispara los efectos, así el VFX llega aunque el objeto que lo originó ya haya sido despawneado. *A persistent `NetworkVFXManager` fires the effects, so VFX still lands even if the source object was already despawned.*
- **Yaw de cámara y ataque en el input / Camera & attack yaw in the input struct** — El servidor calcula movimiento y rotación de ataque sin acceso a la cámara ni al mouse del cliente. *The server computes movement and attack rotation without touching the client's camera or mouse.*
## 🚀 Setup
 
**ES**
1. Clonar el repo y abrir con **Unity 6** (`6000.x`).
2. Cargar el App ID de Photon en `Fusion > Realtime Settings` (crearlo gratis en el [dashboard de Photon](https://dashboard.photonengine.com)).
3. Abrir la escena `Menu` y darle Play. Uno hostea, el resto se une a la misma sala.
4. Para probar en local: build + editor en paralelo, o dos editores con ParrelSync.
**EN**
1. Clone the repo and open it with **Unity 6** (`6000.x`).
2. Set your Photon App ID in `Fusion > Realtime Settings` (free at the [Photon dashboard](https://dashboard.photonengine.com)).
3. Open the `Menu` scene and hit Play. One player hosts, the rest join the same room.
4. To test locally: run a build alongside the editor, or two editors via ParrelSync.
## 📌 Estado / Status
 
**ES —** En desarrollo activo. Próximos pasos: spawner de enemigos y oleadas, y flujo completo de arena/boss.
**EN —** Actively in development. Next up: enemy spawner with waves, and the full arena/boss flow.
 
## 👥 Créditos / Credits
 
Programación: Lautaro Gabriel, Luciana Caminos Cano, Simón Frías
Arte: Fabricio Mettan, Camila Boess
Música y sonido: Simón Frías
Diseño: Luciana Caminos Cano, Fabricio Mettan, Lautaro Gabriel
Producción: Camila Boess
 
