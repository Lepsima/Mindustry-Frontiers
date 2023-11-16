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
        public byte teamCode;
        public string name;
        public Action action;

        public List<Unit> members = new();
        public SquadronUIItem uiItem;

        public byte squadronID;

        public Squadron(byte teamCode, string name, byte id) {
            this.name = name;
            this.teamCode = teamCode;
            squadronID = id;

            if (teamCode == Teams.TeamUtilities.GetLocalTeam()) {
                uiItem = SquadronUI.Instance.Create(this);
            }
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

        public bool IsTeam1() {
            return teamCode == 1;
        }
    }

    public static class SquadronHandler {
        public static string[] squadronNames = new string[] { "Red", "Blue", "Black", "Gold", "Silver", "Razor", "Echo"};
        public const int maxSquadrons = 16;

        public static Squadron[] team1Squadrons = new Squadron[maxSquadrons];
        public static Squadron[] team2Squadrons = new Squadron[maxSquadrons];

        public static List<Unit> nonMemberUnits = new();

        public static Squadron GetSquadronByID(bool team1, byte id) {
            return team1 ? team1Squadrons[id] : team2Squadrons[id];
        }

        public static void CreateSquadrons() {
            for (int i = 0; i < squadronNames.Length; i++) {
                CreateSquadron(1, squadronNames[i]);
                CreateSquadron(2, squadronNames[i]);
            }
        }

        public static void RefreshNonMemberList() {
            nonMemberUnits = new();

            foreach(Unit unit in MapManager.Map.units) {
                if (unit.IsLocalTeam() && unit.squadron == null) nonMemberUnits.Add(unit);
            }
        }

        public static void CreateSquadron(byte teamCode, string name) {
            byte id = 255;

            for (int i = 0; i < maxSquadrons; i++) {
                if ((teamCode == 1 ? team1Squadrons : team2Squadrons)[i] == null) {
                    id = (byte)i;
                    break;
                }
            }

            if (id == 255) return;
            Client.CreateSquadron(teamCode, id, name);
        }

        public static void CreateSquadron(byte teamCode, string name, byte id) {
            Squadron squadron = new(teamCode, name, id);
            (teamCode == 1 ? team1Squadrons : team2Squadrons)[id] = squadron;
        }

        public static void RemoveSquadron(byte teamCode, Squadron squadron) {
            (teamCode == 1 ? team1Squadrons : team2Squadrons)[squadron.squadronID] = null;
            SquadronUI.Instance.Remove(squadron);
        }
    }
}
