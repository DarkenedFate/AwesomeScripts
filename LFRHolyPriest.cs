using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Geometry;
using ReBot.API;
using Newtonsoft.Json;
using System.ComponentModel;

namespace ReBot
{
    [Rotation("LFR Holy Priest - v1.0.0", "Mohn & Jiles", WoWClass.Priest, Specialization.PriestHoly, 30)]
    public class PriestHoly : CombatRotation
    {
  

        public PriestHoly()
        {
            GroupBuffs = new string[]
            {
                "Power Word: Fortitude"
            };
            PullSpells = new string[]
            {
                "Smite",
            };
        }


        public override bool OutOfCombat()
        {
            if (CastSelf("Levitate", () => Me.FallingTime > 2 && !HasAura("Levitate"))) return true;


            // Only use OnWaterMove Spell, if Navi target is not in water. Cancel buff if we have to dive
            if (API.GetNaviTarget() != Vector3.Zero && HasSpell("Levitate"))
            {
                if (!API.IsNaviTargetInWater())
                {
                    if (CastSelf("Levitate", () => Me.Race != WoWRace.Tauren && Me.IsSwimming && !HasAura("Levitate"))) return true;
                }
                else if (HasAura("Levitate"))
                    CancelAura("Levitate");
            }
            if (CastSelfPreventDouble("Greater Heal", () => Me.HealthFraction <= 0.5)) return true;
            if (CastSelfPreventDouble("Flash Heal", () => Me.HealthFraction <= 0.75)) return true;




            if (CastSelf("Power Word: Fortitude", () => !HasAura("Power Word: Fortitude"))) return true;
            if (CastSelf("Fear Ward", () => CurrentBotName == "PvP" && !HasAura("Fear Ward"))) return true;
            if (CastOnTerrain("Angelic Feather", Me.PositionPredicted, () => Me.MovementSpeed > 0 && !HasAura("Angelic Feather"))) return true;

            return false;
        }

        public override void Combat()
        {
            //GLOBAL CD CHECK
            if (HasGlobalCooldown())
                return;


            List<PlayerObject> members = Group.GetGroupMemberObjects();

            if (members.Count > 0)
            {
                var unhealthyMembers = members.Where(x => x.HealthFraction <= 0.7 && x.IsInCombatRange && !x.IsDead).ToList();
                var tanks = members.FindAll(x => x.IsTank && x.IsInCombatRange && !x.IsDead);
                var mainTank = tanks.FirstOrDefault();
                var offTank = tanks.LastOrDefault();

                if (offTank == mainTank)
                    offTank = null;

                if (Cast("Guardian Spirit", () => offTank == null && tanks.FirstOrDefault().HealthFraction <= 0.25)) return;

                if (unhealthyMembers.Count > 5)
                {
                    if (CastOnTerrain("Holy Word: Sanctuary", unhealthyMembers.FirstOrDefault().Position, () => HasAura("Chakra: Sanctuary"))) return;
                    if (Cast("Circle of Healing", () => !Target.IsDead && Target.IsInCombatRange, unhealthyMembers.FirstOrDefault())) return;
                    if (Cast("Prayer of Mending", unhealthyMembers.FirstOrDefault(), () => !Target.IsDead && Target.IsInCombatRange)) return;
                    if (Cast("Renew", unhealthyMembers.FirstOrDefault(), () => !Target.IsDead && Target.IsInCombatRange)) return;
                    if (Cast("Prayer of Healing", unhealthyMembers.FirstOrDefault(), () => !Target.IsDead && Target.IsInCombatRange)) return;
                }
            }

            if (Cast("Renew", () => Me.HealthFraction < 0.80 && !HasAura("Renew"))) return;

            if (CombatMode == CombatModus.Healer)
            {
                foreach (var player in Group.GetGroupMemberObjects().Where(x => x.HealthFraction < 0.9))
                {
                    Cast("Power Word: Shield", player, () => !player.HasAura("Weakened Soul") && !player.HasAura("Power Word: Shield") && player.IsInCombatRange && !player.IsDead);
                    Cast("Flash Heal", () => player.HealthMissing < 10000 || Me.HasAura("Surge of light"), player);
                    Cast("Renew", () => player.HealthFraction < 0.9 && !player.HasAura("Renew"), player);
                    Cast("Binding Heal", player, () => player.HealthFraction < 0.8 && Me.HealthFraction < 0.8);
                    Cast("Flash Heal", player, () => !player.IsTank && !player.IsDead && player.HealthFraction < 0.5 && !player.IsDead && !Me.IsCasting);
                    Cast("Heal", player, () => !player.IsTank && !player.IsDead && player.HealthFraction < 0.8 && player.HealthFraction > 0.4 && !player.IsDead && !Me.IsCasting);
                }

                return;
            }


            Cast("Smite");
        }
    }
}
