using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrunchEconomy.Contracts;
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
        public static bool HandleDeliver(Contract contract, MyPlayer player, PlayerData data, MyCockpit controller)
        {
            var proceed = false;
            switch (contract.type)
            {
                case ContractType.Mining:
                    {
                        if (CrunchEconCore.config.MiningContractsEnabled)
                        {
                            if (contract.minedAmount >= contract.amountToMineOrDeliver)
                            {
                                proceed = true;
                            }
                        }

                        break;
                    }
                case ContractType.Hauling:
                    {
                        if (CrunchEconCore.config.HaulingContractsEnabled)
                        {
                            proceed = true;
                        }

                        break;
                    }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (!proceed) return false;
            if (contract.DeliveryLocation == null && contract.DeliveryLocation == string.Empty)
            {
                contract.DeliveryLocation = DrillPatch.GenerateDeliveryLocation(player.GetPosition(), contract).ToString();
                CrunchEconCore.PlayerStorageProvider.AddContractToBeSaved(contract);
            }

            Vector3D coords = contract.getCoords();
            var rep = 0;
            var distance = Vector3.Distance(coords, controller.PositionComp.GetPosition());
            if (!(distance <= 500)) return false;

            var itemsToRemove = new Dictionary<MyDefinitionId, int>();
            string parseThis;

            if (contract.type == ContractType.Mining)
            {
                parseThis = "MyObjectBuilder_Ore/" + contract.SubType;
                rep = data.MiningReputation;
            }
            else
            {
                parseThis = "MyObjectBuilder_" + contract.TypeIfHauling + "/" + contract.SubType;
                rep = data.HaulingReputation;
            }
            if (MyDefinitionId.TryParse(parseThis, out var id))
            {
                itemsToRemove.Add(id, contract.amountToMineOrDeliver);
            }

            var inventories = InventoryLogic.GetInventoriesForContract(controller.CubeGrid);

            if (!FacUtils.IsOwnerOrFactionOwned(controller.CubeGrid, player.Identity.IdentityId, true)) return false;
            if (!InventoryLogic.ConsumeComponents(inventories, itemsToRemove, player.Id.SteamId)) return false;
            if (contract.type == ContractType.Mining)
            {
                data.MiningReputation += contract.reputation;
                data.MiningContracts.Remove(contract.ContractId);
            }
            else
            {
                data.HaulingReputation += contract.reputation;
                data.HaulingContracts.Remove(contract.ContractId);
            }

            if (data.MiningReputation >= 5000)
            {
                data.MiningReputation = 5000;
            }
            if (data.HaulingReputation >= 5000)
            {
                data.HaulingReputation = 5000;
            }
            if (data.MiningReputation >= 100)
            {
                contract.contractPrice += Convert.ToInt64(contract.contractPrice * 0.025f);
            }
            if (data.MiningReputation >= 250)
            {
                contract.contractPrice += Convert.ToInt64(contract.contractPrice * 0.025f);
            }
            if (data.MiningReputation >= 500)
            {
                contract.contractPrice += Convert.ToInt64(contract.contractPrice * 0.025f);
            }
            if (data.MiningReputation >= 750)
            {
                contract.contractPrice += Convert.ToInt64(contract.contractPrice * 0.025f);
            }
            if (data.MiningReputation >= 1000)
            {
                contract.contractPrice += Convert.ToInt64(contract.contractPrice * 0.025f);
            }
            if (data.MiningReputation >= 2000)
            {
                contract.contractPrice += Convert.ToInt64(contract.contractPrice * 0.025f);
            }
            if (data.MiningReputation >= 3000)
            {
                contract.contractPrice += Convert.ToInt64(contract.contractPrice * 0.05f);
            }
            if (contract.DistanceBonus > 0)
            {
                contract.contractPrice += contract.DistanceBonus;
            }
            if (CrunchEconCore.AlliancePluginEnabled)
            {
                //patch into alliances and process the payment there
                //contract.AmountPaid = contract.contractPrice;
                try
                {
                    var MethodInput = new object[] { player.Id.SteamId, contract.contractPrice, "Mining", controller.CubeGrid.PositionComp.GetPosition() };
                    contract.contractPrice = (long)CrunchEconCore.AllianceTaxes?.Invoke(null, MethodInput);

                }
                catch (Exception ex)
                {
                    CrunchEconCore.Log.Error(ex);
                }
            }

            if (contract.DoRareItemReward)
            {
                foreach (var item in contract.PlayerLoot)
                {
                    if (!MyDefinitionId.TryParse("MyObjectBuilder_" + item.TypeId + "/" + item.SubTypeId,
                            out var reward) || !item.Enabled || rep < item.ReputationRequired) continue;
                    //  Log.Info("Tried to do ");
                    var rand = new Random();
                    var amount = rand.Next(item.ItemMinAmount, item.ItemMaxAmount);
                    var chance = rand.NextDouble();
                    if (!(chance <= item.chance)) continue;
                    if (!InventoryLogic.SpawnLoot(controller.CubeGrid, reward, (MyFixedPoint)amount)) continue;
                    contract.GivenItemReward = true;
                    CrunchEconCore.SendMessage("Boss Dave", $"Heres a bonus for a job well done {amount} {reward.ToString().Replace("MyObjectBuilder_", "")}", Color.Gold, (long)player.Id.SteamId);
                }
            }

            //  BoundingSphereD sphere = new BoundingSphereD(coords, 400);
            var grid = MyAPIGateway.Entities.GetEntityById(contract.StationEntityId) as MyCubeGrid;
            if (grid != null)
            {
                foreach (var item in contract.PutInStation)
                {
                    if (!item.Enabled || rep < item.ReputationRequired) continue;
                    var random = new Random();
                    if (!(random.NextDouble() <= item.chance)) continue;
                    if (!MyDefinitionId.TryParse("MyObjectBuilder_" + item.TypeId + "/" + item.SubTypeId,
                            out var newid)) continue;
                    var amount = random.Next(item.ItemMinAmount, item.ItemMaxAmount);
                    var station = new Stations
                    {
                        CargoName = contract.CargoName,
                        OwnerFactionTag = FacUtils.GetFactionTag(FacUtils.GetOwner(grid)),
                        ViewOnlyNamedCargo = true
                    };
                    InventoryLogic.SpawnItems(grid, newid, amount, station);
                }
                if (contract.PutTheHaulInStation)
                {
                    foreach (var pair in itemsToRemove)
                    {
                        var station = new Stations
                        {
                            CargoName = contract.CargoName,
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
            contract.AmountPaid = contract.contractPrice;
            contract.TimeCompleted = DateTime.Now;
            EconUtils.addMoney(player.Identity.IdentityId, contract.contractPrice);
            contract.PlayerSteamId = player.Id.SteamId;
            contract.status = ContractStatus.Completed;
            CrunchEconCore.PlayerStorageProvider.AddContractToBeSaved(contract, true);
           CrunchEconCore.PlayerStorageProvider.playerData[player.Id.SteamId] = data;

            return true;

            //SAVE THE PLAYER DATA WITH INCREASED REPUTATION

        }

        public static void DoContractDelivery(MyPlayer player, bool DoNewContract)
        {
            if (player.GetPosition() == null) return;

            if (!CrunchEconCore.config.MiningContractsEnabled) return;
            var data = CrunchEconCore.PlayerStorageProvider.GetPlayerData(player.Id.SteamId);
            if (data.MiningContracts.Count <= 0 && data.HaulingContracts.Count <= 0) return;
            var playerOnline = player;
            if (player.Character == null ||
                !(player?.Controller.ControlledEntity is MyCockpit controller)) return;
            var grid = controller.CubeGrid;
            var delete = new List<Contract>();
            delete.AddRange(data.GetMiningContracts().Values.Where(contract => HandleDeliver(contract, player, data, controller)));
            delete.AddRange(data.GetHaulingContracts().Values.Where(contract => HandleDeliver(contract, player, data, controller)));
            foreach (var contract in delete)
            {
                if (contract.type == ContractType.Mining)
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

           CrunchEconCore.PlayerStorageProvider.playerData[player.Id.SteamId] = data;
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
