using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Frontiers.Squadrons {

    public class Action {
        public int action;
        public float radius;
        public Vector2 position;

        public Action(int action, float radius, Vector2 position) {
            this.action = action;
            this.radius = radius;
            this.position = position;
        }

        public Unit.UnitMode ToMode() {
            return (Unit.UnitMode)action;
        }
    }

    public class Squadron {
        public string name;
        public Action action;

        public List<Unit> members = new();
        public SquadronUIItem uiItem;

        public byte squadronID;

        public Squadron(string name, byte id) {
            this.name = name;
            squadronID = id;
            uiItem = SquadronUI.Instance.Create(this);
        }

        public void Add(Unit unit) {
            unit.squadronName = unit.Type.name;
            unit.OnDestroyed += OnMemberDestroyed;

            members.Add(unit);
            uiItem.Add(unit);
        }

        public void Remove(Unit unit) {
            members.Remove(unit);
            uiItem.Remove(unit);
        }

        public void SetAction(Action action) {
            this.action = action;

            foreach(Unit unit in members) {
                unit.SetAction(action);
            }
        }

        public void OnSelected() {
            uiItem.OnSelect();
        }

        public void OnDeselected() {
            uiItem.OnDeselect();
        }

        public void OnMemberDestroyed(object sender, Entity.EntityArg e) {
            Remove(sender as Unit);
            // Message the player about the destruction of a unit
        }

        public short[] GetMembersSyncIDs() {
            short[] arr = new short[members.Count];
            for (int i = 0; i < members.Count; i++) arr[i] = members[i].SyncID;
            return arr;
        }
    }

    public static class SquadronHandler {
        public static string[] squadronNames = new string[] { "Red", "Blue", "Black", "Gold", "Silver", "Razor", "Echo"};
        public const int maxSquadrons = 16;

        public static List<Unit> nonMemberUnits = new();
        public static Squadron[] squadrons = new Squadron[maxSquadrons];

        public static Squadron GetSquadronByID(byte id) {
            return squadrons[id];
        }

        public static void CreateSquadrons() {
            for (int i = 0; i < squadronNames.Length; i++) {
                CreateSquadron(squadronNames[i]);
            }
        }

        public static void RefreshNonMemberList() {
            nonMemberUnits = new();

            foreach(Unit unit in MapManager.Map.units) {
                if (unit.IsLocalTeam() && unit.squadron == null) nonMemberUnits.Add(unit);
            }
        }

        public static void CreateSquadron(string name) {
            byte id = 255;

            for (int i = 0; i < maxSquadrons; i++) {
                if (squadrons[i] == null) {
                    id = (byte)i;
                    break;
                }
            }

            if (id == 255) return;
            Client.CreateSquadron(id, name);
        }

        public static void CreateSquadron(string name, byte id) {
            Squadron squadron = new(name, id);
            squadrons[id] = squadron;
        }

        public static void RemoveSquadron(Squadron squadron) {
            squadrons[squadron.squadronID] = null;
            SquadronUI.Instance.Remove(squadron);
        }
    }
}
