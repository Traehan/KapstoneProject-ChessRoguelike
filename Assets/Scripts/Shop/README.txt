========================================
CURRENCY & SHOP SYSTEM - README
========================================

Welcome to the complete Currency and Shop system!

This system provides a modular, persistent currency
system and a fully functional shop for recruiting
pieces and purchasing upgrades in your roguelike game.

========================================
WHAT'S INCLUDED:
========================================

✓ Currency Management
  • Track coins across scenes
  • Award coins on battle victory
  • Persistent save system
  • Event-driven UI updates

✓ Shop System
  • 3 piece recruit slots
  • 3 upgrade slots
  • Randomized inventory
  • One-time refresh mechanic
  • Integrated with army system

✓ Upgrade System
  • Apply stat boosts to pieces
  • Choose which piece to upgrade
  • Permanent for the run
  • Easy to create new upgrades

✓ Integration
  • Works with GameSession
  • Integrates with PrepPanel
  • Connects to TurnManager
  • Seamless scene transitions

========================================
QUICK START:
========================================

1. READ: QUICKSTART_SHOP.txt
   → 15-minute guided setup

2. FOLLOW: ShopScene_SetupGuide.txt
   → Complete shop scene creation

3. USE: Tools > Shop System menu
   → Editor tools to help setup

4. CREATE: Upgrades
   → Tools > Shop System > Create Upgrade Template

5. TEST: Full game loop
   → Use IMPLEMENTATION_CHECKLIST.txt

========================================
DOCUMENTATION FILES:
========================================

START HERE:
• README.txt (this file)
• QUICKSTART_SHOP.txt

DETAILED GUIDES:
• ShopScene_SetupGuide.txt
• UpgradeDefinitions_README.txt
• CURRENCY_AND_SHOP_SUMMARY.txt

REFERENCE:
• SystemArchitecture_Diagram.txt
• IMPLEMENTATION_CHECKLIST.txt

========================================
SCRIPTS OVERVIEW:
========================================

CURRENCY:
• CurrencyManager.cs
  → Core currency system (singleton)
  → Saves to PlayerPrefs
  → Event-driven updates

• CurrencyDisplay.cs
  → UI component to show coins
  → Add to any scene
  → Auto-updates

• VictoryRewardHandler.cs
  → Awards coins on battle win
  → Add to SampleScene
  → Listens to TurnManager

SHOP:
• ShopManager.cs
  → Main shop controller
  → Manages inventory
  → Handles purchases

• ShopSlot.cs
  → Individual slot UI
  → Shows item info
  → Buy button logic

• UpgradeSelectionPopup.cs
  → Piece selection UI
  → Shows when buying upgrade
  → Lets player choose target

EDITOR TOOLS:
• Editor/CurrencyShopSetupTools.cs
  → Tools menu helpers
  → Quick setup buttons
  → Template creation

========================================
HOW IT WORKS:
========================================

CURRENCY FLOW:
1. Start new run → 0 coins
2. Win battle → +75 coins (configurable)
3. Coins persist across scenes
4. Visit shop → Spend coins
5. Buy pieces/upgrades
6. Return to map with updated army

SHOP FLOW:
1. Enter shop from map
2. See 3 random pieces
3. See 3 random upgrades
4. Buy items (if enough coins)
5. Optionally refresh (25 coins, once)
6. Leave → Return to map

UPGRADE FLOW:
1. Buy upgrade in shop
2. Popup shows your army
3. Select which piece to upgrade
4. Stats permanently increased
5. Upgrade persists in battles

========================================
KEY FEATURES:
========================================

✓ Modular Design
  • Easy to extend
  • Clean separation of concerns
  • Reusable components

✓ Persistent State
  • Currency saves to PlayerPrefs
  • Army updates in GameSession
  • Map state independent

✓ Balanced Economy
  • Configurable prices
  • Configurable rewards
  • One-time refresh limit

✓ Army Integration
  • Purchased pieces added instantly
  • Upgrades modify definitions
  • PrepPanel auto-refreshes

✓ Error-Free
  • No compilation errors
  • Unity 6 compatible
  • Null-safe operations

========================================
EDITOR TOOLS:
========================================

Access via: Tools > Shop System

• Setup Currency Manager
  → Creates CurrencyManager GameObject

• Add Currency Display to Scene
  → Adds coin UI to current scene

• Add Victory Reward Handler
  → Adds to battle scene

• Create Upgrade Template
  → Quick upgrade creation

• Show Setup Guide
  → Opens setup documentation

• Open Currency & Shop Summary
  → Complete system overview

========================================
CUSTOMIZATION:
========================================

ADJUST PRICES:
→ Select ShopManager in Shop_Scene
→ Inspector: Pricing section
→ Change min/max values

ADJUST REWARDS:
→ Select CurrencyManager
→ Inspector: Encounter Victory Reward
→ Default: 75

ADD MORE ITEMS:
→ Select ShopManager
→ Inspector: Available Pieces/Upgrades
→ Add to lists

CHANGE SLOT COUNT:
→ Modify ShopManager script
→ Update scene hierarchy

========================================
SETUP CHECKLIST:
========================================

BASIC SETUP:
□ CurrencyManager created
□ Currency displays added to scenes
□ VictoryRewardHandler in battle scene
□ Upgrades created (3-5 minimum)
□ Shop_Scene created
□ Shop_Scene in Build Settings
□ ShopManager configured
□ All references assigned

TESTING:
□ Currency starts at 0
□ Coins awarded on victory
□ Currency displays update
□ Shop loads correctly
□ Can buy pieces
□ Can buy upgrades
□ Refresh works
□ Leave returns to map
□ Purchases persist

See IMPLEMENTATION_CHECKLIST.txt for complete list!

========================================
TROUBLESHOOTING:
========================================

Q: Currency doesn't display?
A: Check CurrencyManager exists and CurrencyDisplay
   has TextMeshProUGUI reference assigned.

Q: No coins on victory?
A: Verify VictoryRewardHandler is in SampleScene
   and TurnManager.OnPlayerWon fires.

Q: Shop slots empty?
A: Make sure Available Pieces/Upgrades lists
   are populated in ShopManager.

Q: Can't buy items?
A: Check you have enough coins and CurrencyManager
   SpendCoins() is being called.

Q: Upgrades don't apply?
A: Verify piece is in GameSession.I.army and
   isn't the Queen piece.

See ShopScene_SetupGuide.txt for more solutions!

========================================
ARCHITECTURE:
========================================

See SystemArchitecture_Diagram.txt for:
• Component relationships
• Data flow diagrams
• Event chains
• Persistence model
• Lifecycle overview

========================================
NEXT STEPS:
========================================

1. ✓ Read QUICKSTART_SHOP.txt
2. ✓ Create basic shop scene
3. ✓ Test currency system
4. ✓ Create upgrades
5. ✓ Test purchases
6. Polish shop UI
7. Add more pieces/upgrades
8. Balance economy
9. Add visual effects
10. Playtest full loop

========================================
SUPPORT:
========================================

All scripts are well-commented and include:
• XML documentation
• Debug logging
• Null safety checks
• Clear error messages

If you encounter issues:
1. Check Console for error messages
2. Review IMPLEMENTATION_CHECKLIST.txt
3. Follow ShopScene_SetupGuide.txt
4. Verify all references assigned
5. Test in isolation

========================================
VERSION INFO:
========================================

Created for Unity 6000.2
Compatible with:
• Input System 1.14.2
• TextMeshPro (ugui 2.0.0)
• URP 17.2.0

Integrates with your existing:
• GameSession system
• PrepPanel system
• TurnManager system
• SceneController system
• Map system

========================================
CREDITS:
========================================

Currency & Shop System
Created for FinalProjectGameDev

Features:
• Modular currency management
• Dynamic shop inventory
• Piece recruitment system
• Upgrade application system
• Persistent save system
• Event-driven architecture

========================================

Happy building! Your currency and shop system
is ready to use. Follow the quick start guide
to get everything set up in under 15 minutes!

========================================
