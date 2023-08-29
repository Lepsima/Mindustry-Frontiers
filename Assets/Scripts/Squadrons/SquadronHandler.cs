using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frontiers.Squadrons {
    public class Squadron {
        public string name;
        public List<Unit> members = new();
        public SquadronUIItem uiItem;

        public Squadron(string name) {
            this.name = name;
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

        public void OnMemberDestroyed(object sender, Entity.EntityArg e) {
            Remove(sender as Unit);
            // Message the player about the destruction of a unit
        }
    }

    public static class SquadronHandler {
        public static List<Unit> nonMemberUnits = new();
        public static List<Squadron> squadrons = new();

        public static void RefreshNonMemberList() {
            nonMemberUnits = new();

            foreach(Unit unit in MapManager.Map.units) {
                if (unit.IsLocalTeam() && unit.squadron == null) nonMemberUnits.Add(unit);
            }
        }

        public static void CreateSquadron(string name) {
            Squadron squadron = new(name);
            squadrons.Add(squadron);
        }

        public static void RemoveSquadron(Squadron squadron) {
            squadrons.Remove(squadron);
            SquadronUI.Instance.Remove(squadron);
        }
    }
}
