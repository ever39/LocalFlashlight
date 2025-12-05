using ReservedItemSlotCore;
using ReservedItemSlotCore.Data;

namespace LocalFlashlight.Compatibilities
{
    internal class ReservedItemSlotCompatibility
    {
        public static bool flashlightInReservedSlot;
        public static void CheckForFlashlightInSlots()
        {
            for (int i = 0; i <= SessionManager.numReservedItemSlotsUnlocked; i++)
            {
                ReservedItemSlotData tempItemData = SessionManager.GetUnlockedReservedItemSlot(i);
                if (tempItemData != null)
                {
                    var tempObject = tempItemData.GetHeldObjectInSlot(StartOfRound.Instance.localPlayerController);
                    if (tempObject is FlashlightItem && !tempObject.insertedBattery.empty)
                    {
                        var tempFlashlightObj = (FlashlightItem)tempObject;
                        if (tempFlashlightObj.flashlightTypeID != 2)
                        {
                            flashlightInReservedSlot = true;
                            break;
                        }
                    }
                    else flashlightInReservedSlot = false;
                }
            }
        }
    }
}
