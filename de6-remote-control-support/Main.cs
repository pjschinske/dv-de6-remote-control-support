using System;
using System.Reflection;
using HarmonyLib;
using UnityModManagerNet;
using UnityEngine;
using DV.RemoteControls;
using DV.Wheels;
using DV.Simulation.Cars;
using DV.ThingTypes;
using LocoSim.Implementations;
using static UnityModManagerNet.UnityModManager;
using Logger = UnityModManagerNet.UnityModManager.Logger;
using static Oculus.Avatar.CAPI;

namespace de6_remote_control_support;

public class Main
{
	public static ModEntry? mod;

	// Unity Mod Manage Wiki: https://wiki.nexusmods.com/index.php/Category:Unity_Mod_Manager
	private static bool Load(UnityModManager.ModEntry modEntry)
	{
		mod = modEntry;
		Harmony? harmony = null;
		try
		{
			harmony = new Harmony(modEntry.Info.Id);
			harmony.PatchAll(Assembly.GetExecutingAssembly());

			// Other plugin startup logic
		}
		catch (Exception ex)
		{
			modEntry.Logger.LogException($"Failed to load {modEntry.Info.DisplayName}:", ex);
			harmony?.UnpatchAll();
			return false;
		}

		return true;
	}

	[HarmonyPatch(typeof(CarSpawner), nameof(CarSpawner.SpawnCar))]
	class AddRemoteToSpawnedDE6Patch
	{
		static void Postfix(ref TrainCar __result)
		{
			if (__result is object && __result.carType == TrainCarType.LocoDiesel)
			{
				RemoteControllerModule remote = __result.gameObject.AddComponent<RemoteControllerModule>();
				SimController simController = __result.gameObject.GetComponent<SimController>();
				WheelslipController wheelslip = simController.wheelslipController;
				BaseControlsOverrider controls = __result.gameObject.transform.Find("[sim]").GetComponent<BaseControlsOverrider>();
				SimulationFlow simFlow = simController.simFlow;
				remote.Init(__result, wheelslip, controls, simFlow);
				mod?.Logger.Log("Enabled RemoteControllerModule on a spawned DE6");
			}
		}
	}

	[HarmonyPatch(typeof(CarSpawner), nameof(CarSpawner.SpawnLoadedCar))]
	class AddRemoteToLoadedDE6Patch
	{
		static void Postfix(ref TrainCar __result)
		{
			if (__result is object && __result.carType == TrainCarType.LocoDiesel)
			{
				RemoteControllerModule remote = __result.gameObject.AddComponent<RemoteControllerModule>();
				SimController simController = __result.gameObject.GetComponent<SimController>();
				WheelslipController wheelslip = simController.wheelslipController;
				BaseControlsOverrider controls = __result.gameObject.transform.Find("[sim]").GetComponent<BaseControlsOverrider>();
				SimulationFlow simFlow = simController.simFlow;
				remote.Init(__result, wheelslip, controls, simFlow);
				mod?.Logger.Log("Enabled RemoteControllerModule on a loaded DE6");
			}
		}
	}

	[HarmonyPatch(typeof(CarSpawner), nameof(CarSpawner.Awake))]
	class AddRemoteToExistingDE6sPatch
	{
		static void Postfix(CarSpawner __instance)
		{
			foreach (TrainCar loco in __instance.allLocos)
			{
				if (loco is object
					&& loco.carType == TrainCarType.LocoDiesel
					&& loco.gameObject.GetComponent<RemoteControllerModule>() is null)
				{
					RemoteControllerModule remote = loco.gameObject.AddComponent<RemoteControllerModule>();
					SimController simController = loco.gameObject.GetComponent<SimController>();
					WheelslipController wheelslip = simController.wheelslipController;
					BaseControlsOverrider controls = loco.gameObject.transform.Find("[sim]").GetComponent<BaseControlsOverrider>();
					SimulationFlow simFlow = simController.simFlow;
					remote.Init(loco, wheelslip, controls, simFlow);
					mod?.Logger.Log("Enabled RemoteControllerModule on an existing DE6");
				}
			}
		}
	}
}
