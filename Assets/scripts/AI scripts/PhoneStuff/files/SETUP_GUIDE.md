# Phone Texting System - Complete Setup Guide

## Overview
NPCs text the player with weapon requests. The player sends back price and pickup time offers. NPCs interrupt their daily schedule to walk to the meeting location 1 HOUR BEFORE the agreed pickup time.

## Key Features
✅ Random weapon requests from NPCs
✅ Player sets price ($0-$50K) and time (1-60 minutes)
✅ NPCs walk to location 1 in-game hour early
✅ Full conversation history
✅ Integrates with your existing NPC schedule system

## Files Included
1. `TextMessage.cs` - Message data structure
2. `TextConversation.cs` - Conversation management
3. `WeaponOrder.cs` - Weapon order tracking
4. `TextingManager.cs` - Core texting logic (**UPDATED**)
5. `PhoneUI.cs` - Phone interface controller
6. `NPCManager.cs` - **YOUR UPDATED NPC SCRIPT**
7. `DayNightCycleManagerExtension.cs` - Time conversion helpers
8. `WeaponDeliveryInteraction.cs` - Example delivery system

---

## Setup Instructions

### Step 1: Replace Your NPCManager
1. **BACKUP YOUR CURRENT NPCManager.cs**
2. Replace it with the new `NPCManager.cs` provided
3. New features added:
   - `GoingToMeeting` state in NPCState enum
   - `ScheduleWeaponMeeting(Transform location, float time)` method
   - Automatic schedule interruption 1 hour before meeting
   - `IsAtMeetingLocation()` - Check if NPC arrived
   - `CompleteWeaponDeal()` - Resume schedule after delivery

### Step 2: Update DayNightCycleManager
Add these two methods from `DayNightCycleManagerExtension.cs`:

```csharp
public float GetGameTimeFromRealTime(float realTimeSeconds)
{
    float secondsPerFullDay = 1200f; // ⚠️ CHANGE THIS to your day length
    return (realTimeSeconds / secondsPerFullDay) * 24f;
}

public float GetRealTimeFromGameTime(float gameHours)
{
    float secondsPerFullDay = 1200f; // ⚠️ CHANGE THIS to your day length
    return (gameHours / 24f) * secondsPerFullDay;
}
```

⚠️ **CRITICAL:** Change `secondsPerFullDay` to match your actual day/night cycle!
- If 1 day = 20 minutes → `1200f`
- If 1 day = 10 minutes → `600f`
- If 1 day = 30 minutes → `1800f`

### Step 3: Import All Scripts
1. Copy all `.cs` files to your Scripts folder
2. Wait for Unity to compile
3. Fix any compilation errors

### Step 4: Create Meeting Locations
1. Create empty GameObjects around your map
2. Name them clearly (e.g., "Park Bench", "Warehouse Entrance", "Alley")
3. Ensure they're on NavMesh-accessible areas
4. Keep these GameObjects in your scene

### Step 5: Setup TextingManager
1. Create empty GameObject named "TextingManager" in your scene
2. Add `TextingManager` component
3. **Assign ALL NPCs:**
   - Find the "All NPCs" list
   - Set size to number of NPCs in your scene
   - Drag EACH NPCManager GameObject into the list
   - ⚠️ Critical: Every NPC must be in this list!
4. **Assign Weapons:**
   - Add all WeaponData ScriptableObjects to "Available Weapons"
5. **Assign Attachments:**
   - Sight Attachments list
   - Underbarrel Attachments list
   - Barrel Attachments list
   - Magazine Attachments list
   - Side Rail Attachments list
6. **Assign Meeting Locations:**
   - Add your meeting location Transforms
7. **Timing Settings:**
   - Min Time Between Texts: 120 (2 minutes in real-time)
   - Max Time Between Texts: 300 (5 minutes in real-time)

### Step 6: Create UI Hierarchy
In your Canvas, create this structure:

```
Canvas
└── PhonePanel (Panel)
    ├── MainPhonePanel (Panel)
    │   └── TextsButton (Button) ← "Texts" button
    │
    ├── ContactListPanel (Panel) - Initially OFF
    │   ├── HeaderText (TextMeshPro) ← "Messages"
    │   ├── BackButton (Button)
    │   └── ContactScrollView (Scroll View)
    │       └── Viewport
    │           └── ContactListContent (Content)
    │               ├── Vertical Layout Group
    │               └── Content Size Fitter
    │
    ├── ConversationPanel (Panel) - Initially OFF
    │   ├── ConversationHeaderText (TextMeshPro) ← NPC name
    │   ├── BackButton (Button)
    │   ├── MessageScrollView (Scroll View)
    │   │   └── Viewport
    │   │       └── MessageListContent (Content)
    │   │           ├── Vertical Layout Group
    │   │           └── Content Size Fitter
    │   │
    │   └── PriceOfferPanel (Panel) - Initially OFF
    │       ├── PriceLabel (TextMeshPro) ← "Price:"
    │       ├── PriceSlider (Slider)
    │       ├── PriceText (TextMeshPro) ← Shows "$10000"
    │       ├── TimeLabel (TextMeshPro) ← "Time:"
    │       ├── TimeSlider (Slider)
    │       ├── TimeText (TextMeshPro) ← Shows "10 min"
    │       └── SendOfferButton (Button) ← "Send Offer"
```

### Step 7: Create UI Prefabs

#### Contact Button Prefab
1. Right-click in Hierarchy → UI → Button
2. Name it "ContactButtonPrefab"
3. Add Layout Element component
   - Preferred Height: 60
4. Child TextMeshPro: Name it "NameText"
5. Drag to Prefabs folder
6. Delete from scene

#### Player Message Prefab
1. Right-click → UI → Panel
2. Name it "PlayerMessagePrefab"
3. Add Image component (blue/green bubble background)
4. Add child TextMeshPro named "MessageText"
5. Add Horizontal Layout Group
   - Child Alignment: Middle Right
   - Padding: Left 50
6. Optional: Add child "Timestamp" (TextMeshPro)
7. Add Content Size Fitter
   - Vertical Fit: Preferred Size
8. Drag to Prefabs folder
9. Delete from scene

#### NPC Message Prefab
1. Right-click → UI → Panel
2. Name it "NPCMessagePrefab"
3. Add Image component (gray bubble background)
4. Add child TextMeshPro named "MessageText"
5. Add Horizontal Layout Group
   - Child Alignment: Middle Left
   - Padding: Right 50
6. Optional: Add child "Timestamp" (TextMeshPro)
7. Add Content Size Fitter
   - Vertical Fit: Preferred Size
8. Drag to Prefabs folder
9. Delete from scene

### Step 8: Setup PhoneUI Component
1. Select your PhonePanel GameObject
2. Add `PhoneUI` component
3. **Assign Panel References:**
   - Phone Main Panel → MainPhonePanel
   - Contact List Panel → ContactListPanel
   - Conversation Panel → ConversationPanel
   - Price Offer Panel → PriceOfferPanel
4. **Assign Contact List:**
   - Contact List Content → ContactScrollView/Viewport/Content
   - Contact Button Prefab → Your ContactButtonPrefab
5. **Assign Conversation:**
   - Message List Content → MessageScrollView/Viewport/Content
   - Player Message Prefab → Your PlayerMessagePrefab
   - NPC Message Prefab → Your NPCMessagePrefab
   - Conversation Header Text → ConversationHeaderText
6. **Assign Price Offer UI:**
   - Price Slider → PriceSlider
   - Time Slider → TimeSlider
   - Price Text → PriceText
   - Time Text → TimeText
   - Send Offer Button → SendOfferButton

### Step 9: Connect Button Events
1. **TextsButton (on MainPhonePanel):**
   - OnClick() → PhoneUI.OpenContactList
2. **Back buttons:**
   - ContactList BackButton → PhoneUI.BackToMainPhone
   - Conversation BackButton → PhoneUI.BackToContactList
3. **Phone open trigger:**
   - Create a button/key in your game that calls `PhoneUI.Instance.OpenPhone()`

### Step 10: Setup Weapon Delivery (Optional)
1. Add `WeaponDeliveryInteraction.cs` to your Player
2. Create a UI panel for delivery prompt
3. Assign references in the script
4. Implement the inventory check methods:
   - `CheckPlayerHasItems()`
   - `RemoveItemsFromInventory()`
   - `AddMoneyToPlayer()`

---

## How It Works

### 1. Receiving Texts
- TextingManager sends random requests every 2-5 minutes
- Format: "I need a new [Gun] with [Sight], [Magazine] at [Location]."
- New message indicator (•) appears in contact list

### 2. Sending Offers
1. Open contact list
2. Click NPC name
3. Read weapon request
4. Adjust sliders:
   - Price: $0 - $50,000
   - Time: 1-60 minutes
5. Click "Send Offer"
6. NPC auto-accepts

### 3. NPC Goes to Meeting
- NPC calculates arrival time = 1 in-game hour before pickup
- When arrival time hits:
  - NPC state → `GoingToMeeting`
  - Interrupts current schedule
  - Walks to meeting location using NavMesh
- NPC waits at location for 5 real-time minutes

### 4. Weapon Delivery
- Player approaches NPC at location
- Press E (or your interact key)
- Delivery UI shows required items
- Confirm delivery
- Player receives money, NPC resumes schedule

---

## Time Conversion Example

If player sets pickup time to **10 minutes**:
- Real-time: 10 minutes = 600 seconds
- If your day = 20 minutes (1200 seconds):
  - 600 / 1200 * 24 = 12 in-game hours
- If current game time = 9:00 AM:
  - Meeting time = 9:00 PM (21:00)
  - Arrival time = 8:00 PM (20:00)

**NPC will leave for location at 8:00 PM and wait until 9:00 PM.**

---

## Troubleshooting

### NPCs not sending texts
- ✅ TextingManager has all NPCs in "All NPCs" list
- ✅ Available Weapons list populated
- ✅ At least one meeting location assigned
- ✅ NPCs have valid names in npcName field

### NPCs not going to meetings
- ✅ DayNightCycleManager has time conversion methods
- ✅ `secondsPerFullDay` matches your actual day length
- ✅ Meeting locations are on NavMesh
- ✅ NPCs have CivilianMovementController component

### UI not showing
- ✅ All panel GameObjects initially set to inactive
- ✅ PhoneUI component has all references assigned
- ✅ Canvas has Canvas Scaler component
- ✅ EventSystem exists in scene

### Messages not displaying
- ✅ Message prefabs have TextMeshPro components
- ✅ MessageListContent has Vertical Layout Group
- ✅ MessageListContent has Content Size Fitter (Vertical: Preferred)
- ✅ Scroll View has Scroll Rect component

---

## Customization

### Change Text Frequency
In TextingManager:
```csharp
public float minTimeBetweenTexts = 60f;  // 1 minute
public float maxTimeBetweenTexts = 180f; // 3 minutes
```

### Change Price Range
In PhoneUI.Start():
```csharp
priceSlider.maxValue = 100000; // Up to $100K
```

### Change Attachment Probability
In TextingManager.SendRandomWeaponRequest():
```csharp
if (Random.value > 0.5f) // 50% chance instead of 70%
```

### Change Wait Time at Location
In NPCManager:
```csharp
public float meetingWaitTime = 600f; // 10 minutes
```

---

## Integration with Your Inventory

When delivery is successful, implement these methods:

```csharp
// In WeaponDeliveryInteraction.cs

private bool CheckPlayerHasItems()
{
    bool hasWeapon = YourInventory.HasWeapon(currentOrder.weaponRequested);
    bool hasSight = currentOrder.sightAttachment == null || 
                    YourInventory.HasAttachment(currentOrder.sightAttachment);
    // ... check all attachments
    return hasWeapon && hasSight && /* all others */;
}

private void RemoveItemsFromInventory()
{
    YourInventory.RemoveWeapon(currentOrder.weaponRequested);
    if (currentOrder.sightAttachment != null)
        YourInventory.RemoveAttachment(currentOrder.sightAttachment);
    // ... remove all attachments
}

private void AddMoneyToPlayer(float amount)
{
    YourPlayerMoney.Add(amount);
}
```

---

## Requirements Summary

### ScriptableObject Fields
**WeaponData must have:**
```csharp
public string weaponName;
```

**AttachmentData must have:**
```csharp
public string attachmentName;
```

### Scene Requirements
- ✅ All NPCs have NPCManager component
- ✅ All NPCs in TextingManager's list
- ✅ Meeting locations on NavMesh
- ✅ DayNightCycleManager with time methods
- ✅ Canvas with PhoneUI
- ✅ EventSystem in scene

---

## Future Enhancements Ideas
- Multiple deliveries per NPC
- Negotiation (NPC counters price)
- Reputation system affects pricing
- Time bonuses for early delivery
- Penalties for late/missed deliveries
- NPC sends follow-up texts if waiting too long
- Multiple weapons in one order

---

## Need Help?
Check that:
1. All NPCs are in TextingManager list
2. Time conversion matches your day length
3. Meeting locations are accessible
4. All UI references assigned
5. Prefabs have required components

Good luck with your game! 🎮
