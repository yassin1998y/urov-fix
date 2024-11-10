using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Yurowm.GameCore;
using System;
using System.Collections;

// Generator of playing field
public class FieldAssistant : MonoBehaviourAssistant<FieldAssistant> {

    public Transform sceneFolder;

    // Field generator
    public IEnumerator CreateField () {
        sceneFolder = new GameObject("SceneFolder").transform;
        sceneFolder.Reset();

        LevelDesign design = SessionInfo.current.design;

        DelayedAccess access = new DelayedAccess(1f / 20);

        if (SessionInfo.current.rule.slotRenderer) {
            SlotRenderer slotRenderer = Content.Emit(SessionInfo.current.rule.slotRenderer);
            slotRenderer.transform.SetParent(sceneFolder);
            slotRenderer.transform.Reset();
            slotRenderer.Rebuild(design.slots.Select(x => x.position).ToList());
            if (access.GetAccess()) yield return 0;
        }

        Slot.all.Clear();

        Dictionary<string, ISlotContent> content = Content.GetPrefabList<ISlotContent>()
            .ToDictionary(x => x.name, x => x);


        Slot slotPrefab = Content.GetPrefab<Slot>();

        if (!slotPrefab)
            throw new Exception("Slot prefab is missing");

        foreach (SlotSettings settings in design.slots) {

            #region Creating a new empty slot
            Vector3 position = new Vector3(.5f + settings.position.x, .5f + settings.position.y, 0) * Project.main.slot_offset;
            GameObject obj = Content.Emit(slotPrefab).gameObject;
            obj.name = "Slot_" + settings.position.x + "x" + settings.position.y;
            obj.transform.position = position;
            obj.transform.SetParent(sceneFolder);
            Slot slot = obj.GetComponent<Slot>();
            slot.position = settings.position;
            Slot.all.Add(slot.position, slot);
            if (access.GetAccess()) yield return 0;
            #endregion

            foreach (SlotContent c in settings.content)
                if (content.ContainsKey(c.name)) {
                    Content.Emit(content[c.name]).Setup(slot);
                    if (access.GetAccess()) yield return 0;
                }
        }
        
        Slot.InitializeAll();

        foreach (BigObjectSettings settings in design.bigObjects) {
            if (content.ContainsKey(settings.content.name)) {
                Slot slot = Slot.all.Get(settings.position);
                (Content.Emit(content[settings.content.name]) as IBigObject).BigSetup(slot);
                if (access.GetAccess()) yield return 0;
            }
        }

        foreach (ISlotContent prefab in content.Values.Where(x => x is INeedToBeSetup)) {
            List<ISlotContent> items = ILiveContent.GetAll<ISlotContent>(x => x.GetType() == prefab.GetType());
            if (items.Count > 0) {
                Dictionary<int2, SlotContent> infos = prefab is IBigObject ? 
                    design.bigObjects.Where(x => x.content.name == prefab.name).ToDictionary(x => x.position, x => x.content) :                    
                    design.slots.ToDictionary(x => x.position, x => x.content.FirstOrDefault(y => y.name == prefab.name)).RemoveAll(x => x.Value == null);
                items.Where(x => infos.ContainsKey(x.slot.position)).ForEach(x => (x as INeedToBeSetup).OnSetupByContentInfo(x.slot, infos[x.slot.position]));
                if (access.GetAccess()) yield return 0;
            }
            if (access.GetAccess()) yield return 0;
        }

        foreach (var info in design.extensions) {
            ILevelExtension extension = Content.Emit(info.prefab);
            if (extension) {
                extension.info = info;
                extension.Setup(info);
                if (access.GetAccess()) yield return 0;
            }
        }
    }
    
	public void  RemoveField (){
        ILiveContent.KillEverything();
        if (sceneFolder)
            Destroy(sceneFolder.gameObject);
    }

	public ISlotContent CreateNewContent (ISlotContent reference, Slot slot, Vector3 positionOffset = default(Vector3), ItemColor color = ItemColor.Unknown) {
        if (!slot) return null;

        ISlotContent content = Content.Emit(reference).GetComponent<ISlotContent>();
        content.name = reference.name;

        if (content && content is IColored)
            ISlotContent.Repaint(content, color);

        if (slot.chip) 
            content.transform.position = slot.chip.transform.position;
        else
            content.transform.position = slot.transform.position + positionOffset;

        slot.AttachContent(content);
        content.transform.SetParent(slot.transform);

        content.slot = slot;

        if (content is INeedToBeSetup)
            (content as INeedToBeSetup).OnSetup(slot);

        return content;
	}

    #region Add Content
    public ISlotContent Add(ISlotContent reference, Slot slot = null, ItemColor color = ItemColor.Unknown) {
        if (!slot) {
            List<Slot> targets = new List<Slot>(Slot.allActive.Values);
            if (reference is IChip || reference is IBlock)
                targets.RemoveAll(x => x.block || !x.chip || !(x.chip is IDefaultSlotContent));
            else if (reference is ISlotModifier) {
                Type modifierType = reference.GetType();
                targets.RemoveAll(x => x.CheckModifier(m => m.GetType() == modifierType));
            }
            if (targets.Count > 0) slot = targets.GetRandom();
        }

        if (!slot) return null;

        ISlotContent current = null;
        if (reference is IChip) current = slot.chip;
        else if (reference is IBlock) current = slot.block;

        ItemColor reference_color = color;
        if (current && current.colored != null)
            if (reference_color == ItemColor.Unknown)
                reference_color = current.colored.color;

        if (current) {
            Project.onSlotContentPrepareToDestroy.Invoke(current);
            current.Hide();
        }

        current = CreateNewContent(reference, slot, Vector3.zero, reference_color);

		return current;
    }

    public ISlotContent Add(ISlotContent reference, int2 slotCoord, ItemColor color = ItemColor.Unknown) {
        Slot slot = Slot.all.Get(slotCoord);
        if (!slot) return null;

        return Add(reference, slot, color);
    }

    public ISlotContent Add<T>(Slot slot = null, ItemColor color = ItemColor.Unknown) where T : ISlotContent {
        ISlotContent reference = Content.GetPrefab<T>();
        if (!reference) return null;
        return Add(reference, slot, color);
    }

    public ISlotContent Add<T>(int2 slotCoord, ItemColor color = ItemColor.Unknown) where T : ISlotContent {
        Slot slot = Slot.all.Get(slotCoord);
        if (!slot) return null;
        return Add<T>(slot, color);
    }
    #endregion
}