using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Yurowm.GameCore;

[RequireComponent (typeof (BorderRenderer))]
public class Wall : ILevelExtension {
    public static readonly string vertKey = "v";
    public static readonly string horizKey = "h";

    public override void Setup(LevelExtensionInfo info) {
        Dictionary<int2, SlotSettings> slots = SessionInfo.current.design.slots.ToDictionary(x => x.position, x => x);
        List<int2> vertWall = new List<int2>();
        List<int2> horizWall = new List<int2>();
        foreach (Slot slot in Slot.all.Values) {
            foreach (Side side in Utils.straightSides) {
                if (!Slot.all.ContainsKey(slot.position + side)) continue;
                if (IsWall(slot.position, side, slots, info)) {
                    slot.nearSlot[side] = null;
                    if (side == Side.Top) horizWall.Add(slot.position + int2.up);
                    else if (side == Side.Left) vertWall.Add(slot.position);
                }
            }
        }
        Slot.all.Values.ForEach(x => x.CulculateFallingSlot());
        GetComponent<BorderRenderer>().Rebuild(vertWall, horizWall);        
    }

    public static bool IsWall(int2 coord, Side side, Dictionary<int2, SlotSettings> slots, LevelExtensionInfo info) {
        Coord wall = GetCoord(coord, side, slots);
        if (!wall.exist)
            return false;

        SlotExtension slot = info[wall.coord];
        if (slot == null) return false;

        return slot[wall.vert ? vertKey : horizKey].Bool;
    }

    public static Coord GetCoord(int2 coord, Side side, Dictionary<int2, SlotSettings> slots) {
        Coord result = new Coord();
        result.exist = true;

        if (!side.IsStraight() || !slots.ContainsKey(coord + side)) {
            result.exist = false;
            return result;
        }

        result.coord = coord;
        result.vert = side.Y() == 0;
        if ((result.vert ? side.X() : side.Y()) > 0) result.coord += side;

        return result;
    }

    public struct Coord {
        public bool exist;
        public int2 coord;
        public bool vert;
    }
}
