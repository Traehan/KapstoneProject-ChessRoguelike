========================================
HOW TO CREATE PIECE UPGRADES
========================================

Piece upgrades are ScriptableObjects that can be purchased in the shop
and applied to pieces in your army.

========================================
CREATING AN UPGRADE:
========================================

1. In Unity, right-click in the Project window
2. Go to: Create > Upgrades
3. Name your upgrade (e.g., "IronPlating", "SharpenedBlade")
4. Configure the upgrade in the Inspector:

   Display Name: "Iron Plating" (shown in shop)
   Description: "+2 HP to the selected piece"
   Icon: Assign a sprite
   
   Add Max HP: 2
   Add Attack: 0
   
   Keyword Ability: Leave null for stat-only upgrades

========================================
EXAMPLES OF UPGRADES TO CREATE:
========================================

STAT UPGRADES:
--------------
1. Iron Plating
   - Display Name: "Iron Plating"
   - Description: "Increases piece HP by 2"
   - Add Max HP: 2
   - Add Attack: 0

2. Sharpened Blade
   - Display Name: "Sharpened Blade"
   - Description: "Increases piece Attack by 1"
   - Add Max HP: 0
   - Add Attack: 1

3. Fortified Armor
   - Display Name: "Fortified Armor"
   - Description: "Greatly increases HP"
   - Add Max HP: 3
   - Add Attack: 0

4. Combat Training
   - Display Name: "Combat Training"
   - Description: "Balanced stat increase"
   - Add Max HP: 1
   - Add Attack: 1

5. Berserker Rage
   - Display Name: "Berserker Rage"
   - Description: "Massive attack boost"
   - Add Max HP: 0
   - Add Attack: 2

========================================
ASSIGNING TO SHOP:
========================================

1. Open the Shop scene
2. Select the ShopManager GameObject
3. In the Inspector, find "Available Upgrades"
4. Increase the size
5. Drag your created upgrade ScriptableObjects into the slots

========================================
HOW UPGRADES WORK:
========================================

When a player purchases an upgrade:
1. They spend coins
2. A popup appears showing all pieces in their army (except Queen)
3. They select which piece to apply the upgrade to
4. The selected piece's stats are permanently increased
5. The upgrade persists for the entire run
6. Stats are applied immediately and saved in the PieceDefinition

========================================
UPGRADE PERSISTENCE:
========================================

Upgrades modify the PieceDefinition directly, so they:
- Persist across battles in the same run
- Are saved in the GameSession.army list
- Reset when a new run starts
- Apply to all instances of that piece in battle

========================================
