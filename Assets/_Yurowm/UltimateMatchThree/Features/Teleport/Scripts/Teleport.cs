using UnityEngine;
using System.Collections;
using Yurowm.GameCore;
using System;
using System.Linq;
using System.Xml.Linq;

[SlotTagRenderer('T', ConsoleColor.Cyan, ConsoleColor.Black)]
public class Teleport : ISlotModifier, INeedToBeSetup {

    public static bool IsTagVisible(SlotSettings slot) {
        return slot.content.FirstOrDefault(x => x.name == "Teleport") != null;
    }

    public Transform enter;
    public Transform exit;

    public Slot target;

	public int2 target_postion = null;

	float lastTime = -10;
	float delay = 0.15f; // delay between the generations

	void  Update (){
        if (!SessionInfo.current.isPlaying) return;

        if (SessionInfo.current.rule.GetMode() != PlayingMode.Gravity) return;
		
		if (!slot.chip) return; // Teleport is possible only if slot contains chip

		if (target.chip) return; // Teleport is impossible if target slot already contains chip
				
		if (slot.block) return; // Teleport is impossible, if the slot is blocked
		if (target.block) return; // Teleport is impossible, if the target slot is blocked

		if (lastTime + delay > Time.time) return; // limit of frequency generation
		lastTime = Time.time;
		
		if ((slot.chip.transform.position - slot.transform.position).sqrMagnitude >
            (Project.main.slot_offset * Project.main.slot_offset) / 6) return;

		TeleportChip (slot.chip, target);
	}

    public void OnSetupByContentInfo(Slot slot, SlotContent info) {
        int2 target_coord = info["target"].Coordinate;
        if (target_coord != null && Slot.all.ContainsKey(target_coord)) {
            target_postion = target_coord;
            target = Slot.all.Get(target_postion);
            exit.transform.SetParent(target.transform);
            exit.transform.localPosition = Vector3.zero;
        }
        if (!target) {
            slot.DetachContent(this);
            Kill();
        }
    }

    public void OnSetup(Slot slot) {}

    public override void Serialize(XElement xContent) {
        xContent.Add(new XAttribute("target", target.position.ToString()));
    }

    public override void Deserialize(XElement xContent, SlotContent slotContent) {
        slotContent["target"].Coordinate = int2.Parse(xContent.Attribute("target").Value);
    }

    void TeleportChip(IChip chip, Slot target) {
        if (!chip.slot) return;
        if (chip is IDestroyable && (chip as IDestroyable).destroying) return;
        if (target.chip || (target.block && !target.block.CanItContainChip())) return;

        target.chip = chip;
        chip.transform.position = target.transform.position;
    }
}
