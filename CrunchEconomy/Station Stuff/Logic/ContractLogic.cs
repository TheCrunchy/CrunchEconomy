using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrunchEconomy.Contracts;
using CrunchEconomy.Helpers;
using CrunchEconomy.Station_Stuff.Objects;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRageMath;

namespace CrunchEconomy.Station_Stuff.Logic
{
    public static class ContractLogic
    {
        public static bool HandleDeliver(Contract Contract, MyPlayer Player, PlayerData Data, MyCockpit Controller)
        {
            var proceed = false;
            switch (Contract.Type)
            {
                case ContractType.Mining:
                    {
                        if (CrunchEconCore.Config.MiningContractsEnabled)
                        {
                            if (Contract.MinedAmount >= Contract.AmountToMineOrDeliver)
                            {
                                proceed = true;
                            }
                        }

                        break;
                    }
                case ContractType.Hauling:
                    {
                        if (CrunchEconCore.Config.HaulingContractsEnabled)
                        {
                            proceed = true;
                        }

                        break;
                    }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (!proceed) return false;
            if (Contract.DeliveryLocation == null && Contract.DeliveryLocation == string.Empty)
            {
                Contract.DeliveryLocation = DrillPatch.GenerateDeliveryLocation(Player.GetPosition(), Contract).ToString();
                CrunchEconCore.PlayerStorageProvider.AddContractToBeSaved(Contract);
            }

            Vector3D coords = Contract.GetCoords();
            var rep = 0;
            var distance = Vector3.Distance(coords, Controller.PositionComp.GetPosition());
            if (!(distance <= 500)) return false;

            var itemsToRemove = new Dictionary<MyDefinitionId, int>();
            string parseThis;

            if (Contract.Type == ContractType.Mining)
            {
                parseThis = "MyObjectBuilder_Ore/" + Contract.SubType;
                rep = Data.MiningReputation;
            }
            else
            {
                parseThis = "MyObjectBuilder_" + Contract.TypeIfHauling + "/" + Contract.SubType;
                rep = Data.HaulingReputation;
            }
            if (MyDefinitionId.TryParse(parseThis, out var id))
            {
                itemsToRemove.Add(id, Contract.AmountToMineOrDeliver);
            }

            var inventories = InventoryLogic.GetInventoriesForContract(Controller.CubeGrid);

            if (!FacUtils.IsOwnerOrFactionOwned(Controller.CubeGrid, Player.Identity.IdentityId, true)) return false;
            if (!InventoryLogic.ConsumeComponents(inventories, itemsToRemove, Player.Id.SteamId)) return false;
            if (Contract.Type == ContractType.Mining)
            {
                Data.MiningReputation += Contract.Reputation;
                Data.MiningContracts.Remove(Contract.ContractId);
            }
            else
            {
                Data.HaulingReputation += Contract.Reputation;
                Data.HaulingContracts.Remove(Contract.ContractId);
            }

            if (Data.MiningReputation >= 5000)
            {
                Data.MiningReputation = 5000;
            }
            if (Data.HaulingReputation >= 5000)
            {
                Data.HaulingReputation = 5000;
            }
            if (Data.MiningReputation >= 100)
            {
                Contract.ContractPrice += Convert.ToInt64(Contract.ContractPrice * 0.025f);
            }
            if (Data.MiningReputation >= 250)
            {
                Contract.ContractPrice += Convert.ToInt64(Contract.ContractPrice * 0.025f);
            }
            if (Data.MiningReputation >= 500)
            {
                Contract.ContractPrice += Convert.ToInt64(Contract.ContractPrice * 0.025f);
            }
            if (Data.MiningReputation >= 750)
            {
                Contract.ContractPrice += Convert.ToInt64(Contract.ContractPrice * 0.025f);
            }
            if (Data.MiningReputation >= 1000)
            {
                Contract.ContractPrice += Convert.ToInt64(Contract.ContractPrice * 0.025f);
            }
            if (Data.MiningReputation >= 2000)
            {
                Contract.ContractPrice += Convert.ToInt64(Contract.ContractPrice * 0.025f);
            }
            if (Data.MiningReputation >= 3000)
            {
                Contract.ContractPrice += Convert.ToInt64(Contract.ContractPrice * 0.05f);
            }
            if (Contract.DistanceBonus > 0)
            {
                Contract.ContractPrice += Contract.DistanceBonus;
            }
            if (CrunchEconCore.AlliancePluginEnabled)
            {
                //patch into alliances and process the payment there
                //contract.AmountPaid = contract.contractPrice;
                try
                {
                    var methodInput = new object[] { Player.Id.SteamId, Contract.ContractPrice, "Mining", Controller.CubeGrid.PositionComp.GetPosition() };
                    Contract.ContractPrice = (long)CrunchEconCore.AllianceTaxes?.Invoke(null, methodInput);

                }
                catch (Exception ex)
                {
                    CrunchEconCore.Log.Error(ex);
                }
            }

            if (Contract.DoRareItemReward)
            {
                foreach (var item in Contract.PlayerLoot)
                {
                    if (!MyDefinitionId.TryParse("MyObjectBuilder_" + item.TypeId + "/" + item.SubTypeId,
                            out var reward) || !item.Enabled || rep < item.ReputationRequired) continue;
                    //  Log.Info("Tried to do ");
                    var rand = new Random();
                    var amount = rand.Next(item.ItemMinAmount, item.ItemMaxAmount);
                    var chance = rand.NextDouble();
                    if (!(chance <= item.Chance)) continue;
                    if (!InventoryLogic.SpawnLoot(Controller.CubeGrid, reward, (MyFixedPoint)amount)) continue;
                    Contract.GivenItemReward = true;
                    CrunchEconCore.SendMessage("Boss Dave", $"Heres a bonus for a job well done {amount} {reward.ToString().Replace("MyObjectBuilder_", "")}", Color.Gold, (long)Player.Id.SteamId);
                }
            }

            //  BoundingSphereD sphere = new BoundingSphereD(coords, 400);
            var grid = MyAPIGateway.Entities.GetEntityById(Contract.StationEntityId) as MyCubeGrid;
            if (grid != null)
            {
                foreach (var item in Contract.PutInStation)
                {
                    if (!item.Enabled || rep < item.ReputationRequired) continue;
                    var random = new Random();
                    if (!(random.NextDouble() <= item.Chance)) continue;
                    if (!MyDefinitionId.TryParse("MyObjectBuilder_" + item.TypeId + "/" + item.SubTypeId,
                            out var newid)) continue;
                    var amount = random.Next(item.ItemMinAmount, item.ItemMaxAmount);
                    var station = new Stations
                    {
                        CargoName = Contract.CargoName,
                        OwnerFactionTag = FacUtils.GetFactionTag(FacUtils.GetOwner(grid)),
                        ViewOnlyNamedCargo = true
                    };
                    InventoryLogic.SpawnItems(grid, newid, amount, station);
                }
                if (Contract.PutTheHaulInStation)
                {
                    foreach (var pair in itemsToRemove)
                    {
                        var station = new Stations
                        {
                            CargoName = Contract.CargoName,
                            OwnerFactionTag = FacUtils.GetFactionTag(FacUtils.GetOwner(grid)),
                            ViewOnlyNamedCargo = true
                        };
                        InventoryLogic.SpawnItems(grid, pair.Key, pair.Value, station);
                    }
                }

            }
            else
            {
                CrunchEconCore.Log.Error("Couldnt find station to put items in! Did it get cut and pasted? at " + coords.ToString());
            }
            Contract.AmountPaid = Contract.ContractPrice;
            Contract.TimeCompleted = DateTime.Now;
            EconUtils.AddMoney(Player.Identity.IdentityId, Contract.ContractPrice);
            Contract.PlayerSteamId = Player.Id.SteamId;
            Contract.Status = ContractStatus.Completed;
            CrunchEconCore.PlayerStorageProvider.AddContractToBeSaved(Contract, true);
           CrunchEconCore.PlayerStorageProvider.PlayerData[Player.Id.SteamId] = Data;

            return true;

            //SAVE THE PLAYER DATA WITH INCREASED REPUTATION

        }

        public static void DoContractDelivery(MyPlayer Player, bool DoNewContract)
        {
            if (Player.GetPosition() == null) return;

            if (!CrunchEconCore.Config.MiningContractsEnabled) return;
            var data = CrunchEconCore.PlayerStorageProvider.GetPlayerData(Player.Id.SteamId);
            if (data.MiningContracts.Count <= 0 && data.HaulingContracts.Count <= 0) return;
            var playerOnline = Player;
            if (Player.Character == null ||
                !(Player?.Controller.ControlledEntity is MyCockpit controller)) return;
            var grid = controller.CubeGrid;
            var delete = new List<Contract>();
            delete.AddRange(data.GetMiningContracts().Values.Where(Contract => HandleDeliver(Contract, Player, data, controller)));
            delete.AddRange(data.GetHaulingContracts().Values.Where(Contract => HandleDeliver(Contract, Player, data, controller)));
            foreach (var contract in delete)
            {
                if (contract.Type == ContractType.Mining)
                {
                    data.GetMiningContracts().Remove(contract.ContractId);
                    data.MiningContracts.Remove(contract.ContractId);
                }
                else
                {
                    data.GetHaulingContracts().Remove(contract.ContractId);
                    data.MiningContracts.Remove(contract.ContractId);
                }
            }

            CrunchEconCore.PlayerStorageProvider.PlayerData[Player.Id.SteamId] = data;
            try
            {
                CrunchEconCore.PlayerStorageProvider.SavePlayerData(data);
            }
            catch (Exception ex)
            {
                CrunchEconCore.Log.Error("WHY YOU DO THIS?");
            }
        }

    }
}
