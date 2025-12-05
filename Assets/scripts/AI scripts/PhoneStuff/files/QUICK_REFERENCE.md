# Quick Reference Card

## 🎯 System Flow

```
1. TextingManager sends request → NPC texts player
2. Player opens phone → Sees message with weapon/attachments/location
3. Player sets price & time → Sends offer
4. NPC auto-accepts → Schedules meeting
5. 1 in-game hour before meeting → NPC walks to location
6. Player delivers weapon → Gets paid, NPC resumes schedule
```

## 📋 Critical Setup Checklist

### ✅ Before You Start
- [ ] Backup your current NPCManager.cs
- [ ] Know your day/night cycle length in seconds
- [ ] Have all NPCs in scene
- [ ] Have meeting locations marked

### ✅ Scripts Added
- [ ] Replaced NPCManager.cs
- [ ] Added time methods to DayNightCycleManager
- [ ] Imported all other .cs files
- [ ] No compilation errors

### ✅ TextingManager Setup
- [ ] GameObject created in scene
- [ ] ALL NPCs added to "All NPCs" list (count matches scene)
- [ ] All weapons added
- [ ] All 5 attachment lists filled
- [ ] Meeting location Transforms added
- [ ] Timing values set

### ✅ UI Created
- [ ] PhonePanel hierarchy built
- [ ] 3 prefabs created (Contact, Player Message, NPC Message)
- [ ] PhoneUI component added
- [ ] All 20+ references assigned
- [ ] Buttons connected
- [ ] Phone open trigger created

## 🔧 Common Issues & Fixes

| Problem | Solution |
|---------|----------|
| NPCs not texting | Add all NPCs to TextingManager list |
| Time wrong | Fix `secondsPerFullDay` in time methods |
| NPC won't walk | Check NavMesh at meeting locations |
| UI not showing | Check all references in PhoneUI |
| No messages visible | Add Vertical Layout Group to Content |

## 📱 Key Methods to Remember

### For Player Interaction
```csharp
// Open phone
PhoneUI.Instance.OpenPhone();

// Check if NPC ready for delivery
TextingManager.Instance.IsNPCReadyForDelivery(npcName);

// Complete delivery
TextingManager.Instance.CompleteWeaponDelivery(npcName);
```

### For Custom Behavior
```csharp
// Force send request (testing)
TextingManager.Instance.SendRandomWeaponRequest();

// Get active order details
WeaponOrder order = TextingManager.Instance.GetActiveOrderForNPC(npcName);

// Check NPC state
NPCManager npc = TextingManager.Instance.GetNPCByName(npcName);
bool atLocation = npc.IsAtMeetingLocation();
```

## ⚙️ Quick Customization

### Change text frequency (TextingManager)
```csharp
public float minTimeBetweenTexts = 60f;   // seconds
public float maxTimeBetweenTexts = 180f;  // seconds
```

### Change price range (PhoneUI.Start)
```csharp
priceSlider.maxValue = 100000; // max price
```

### Change meeting wait time (NPCManager)
```csharp
public float meetingWaitTime = 300f; // seconds
```

### Change arrival time (NPCManager.ScheduleWeaponMeeting)
```csharp
arrivalTime = meetingTime - 2f; // 2 hours early instead of 1
```

## 🎮 Testing Steps

1. **Test Text Sending**
   - Play game
   - Wait 2-5 minutes or call `SendRandomWeaponRequest()` manually
   - Check phone shows new message

2. **Test Offer Sending**
   - Open contact
   - Set price/time
   - Send offer
   - Check NPC responds

3. **Test NPC Movement**
   - Send offer with short time (2-3 min)
   - Watch NPC leave schedule and walk to location
   - Verify NPC arrives ~1 hour before meeting time

4. **Test Delivery**
   - Approach NPC at location
   - Press interact key
   - Confirm delivery
   - Check money received
   - Verify NPC resumes schedule

## 📊 Required ScriptableObject Fields

```csharp
// WeaponData.cs
public string weaponName; // ← Must have this!

// AttachmentData.cs  
public string attachmentName; // ← Must have this!
```

## 🚀 Quick Start (Minimal Testing)

1. Add 1 NPC to TextingManager
2. Add 1 weapon
3. Add 1 attachment to any slot
4. Add 1 meeting location
5. Create basic UI (just contact list + 1 conversation)
6. Test!

## 📞 Phone Controls Summary

```
Main Phone
  └─ [Texts Button] → Opens Contact List

Contact List  
  └─ [NPC Name] → Opens Conversation
  
Conversation
  ├─ Shows messages
  ├─ Price Slider ($0-$50K)
  ├─ Time Slider (1-60 min)
  └─ [Send Offer] → NPC accepts & schedules meeting
```

## 💡 Pro Tips

- **Name meeting locations clearly** - Players will see these names
- **Test time conversion** - Set pickup to 1 minute, verify NPC leaves on time
- **Use Debug.Log** - NPCManager logs all state changes
- **Spread locations** - Don't cluster all meeting spots
- **Balance timing** - Too frequent = spam, too rare = boring
- **Test NavMesh** - Walk to each meeting location manually first

## 🎨 UI Polish Ideas

- Add notification sound when text received
- Add "typing..." indicator before NPC responds
- Highlight unread messages with color
- Show money in delivery UI
- Add "Are you sure?" confirmation
- Display time until NPC arrives at location
- Show NPC portrait in conversation header

## 📝 Files You Modified

1. `NPCManager.cs` - REPLACED YOUR VERSION
2. `DayNightCycleManager.cs` - ADDED 2 METHODS
3. Your scene - ADDED TextingManager + UI

Everything else is new!

---

Save this card for quick reference during development! 🎯
