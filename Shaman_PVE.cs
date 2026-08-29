using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using robotManager.Helpful;
using wManager.Wow.Class;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: CompilationRelaxations(8)]
[assembly: AssemblyVersion("0.0.0.0")]
public enum TotemRestoreAction
{
    NONE,
    GLOBAL_CALL,
    PARTIAL_FALLBACK
}

public class Main : ICustomClass
{
	private struct ShamanCapabilities
	{
		public bool Has4T10;

		public bool HasGlyphFlameShock;

		public bool HasGlyphLava;

		public bool HasElementalMastery;

		public bool HasThunderstorm;

		public bool HasFireNova;

		public bool HasFireElemental;

		public bool HasChainLightning;

		public bool HasLavaBurst;

		public bool HasFlameShock;

		public bool HasLightningBolt;

		public void Update(Main m)
		{
			Has4T10 = Lua.LuaDoString<bool>("\n                local count = 0;\n                local t10 = {50841,50842,50843,50844,50845, 51167,51168,51169,51170,51171, 51240,51241,51242,51243,51244};\n                for i=1,19 do\n                    local link = GetInventoryItemLink('player', i);\n                    if link then\n                        for _, id in ipairs(t10) do\n                            if link:find('item:'..id) then\n                                count = count + 1;\n                                break;\n                            end\n                        end\n                    end\n                end\n                return count >= 4;\n            ", "");
			HasGlyphFlameShock = Lua.LuaDoString<bool>("for i=1,6 do local e,_,_,id = GetGlyphSocketInfo(i); if e and id == 55447 then return true; end end return false;", "");
			HasGlyphLava = Lua.LuaDoString<bool>("for i=1,6 do local e,_,_,id = GetGlyphSocketInfo(i); if e and id == 55455 then return true; end end return false;", "");
			HasElementalMastery = m.ResolveSpell("elemental_mastery") != 0;
			HasThunderstorm = m.ResolveSpell("thunderstorm") != 0;
			HasFireNova = m.ResolveSpell("fire_nova") != 0;
			HasFireElemental = m.ResolveSpell("fire_elemental_totem") != 0;
			HasChainLightning = m.ResolveSpell("chain_lightning") != 0;
			HasLavaBurst = m.ResolveSpell("lava_burst") != 0;
			HasFlameShock = m.ResolveSpell("flame_shock") != 0;
			HasLightningBolt = m.ResolveSpell("lightning_bolt") != 0;
		}
	}

	private struct ProcState
	{
		public bool HasBloodlust;

		public bool HasPotionActive;

		public bool HasTrinketProc;

		public bool HasClearcasting;

		public int ActiveProcsCount;

		public float SnapshotScore;

		public void Update()
		{
			string text = Lua.LuaDoString<string>("\n                local score = 0;\n                local count = 0;\n                local bl = 0; local pot = 0; local trinket = 0; local cc = 0;\n                for i=1,40 do\n                    local name,_,_,_,_,_,_,_,_,_,id = UnitBuff('player', i);\n                    if not name then break end\n                    if id == 2825 or id == 32182 then bl = 1; score = score + 500; count = count + 1; end\n                    if id == 16246 then cc = 1; end\n                    if id == 53908 or id == 53909 then pot = 1; score = score + 200; count = count + 1; end\n                    local trinkets = {59077, 64713, 60492, 60494, 71570, 71572, 72416, 75466, 65006, 65011, 64712, 64713, 59626, 67669, 67671};\n                    for _, tid in ipairs(trinkets) do\n                        if id == tid then trinket = 1; score = score + 300; count = count + 1; break; end\n                    end\n                end\n                return tostring(bl) .. '|' .. tostring(pot) .. '|' .. tostring(trinket) .. '|' .. tostring(cc) .. '|' .. tostring(count) .. '|' .. tostring(score);\n            ", "");
			if (!string.IsNullOrEmpty(text))
			{
				string[] array = text.Split('|');
				if (array.Length == 6)
				{
					HasBloodlust = array[0] == "1";
					HasPotionActive = array[1] == "1";
					HasTrinketProc = array[2] == "1";
					HasClearcasting = array[3] == "1";
					ActiveProcsCount = int.Parse(array[4]);
					SnapshotScore = float.Parse(array[5]);
				}
			}
		}
	}

	private struct HealthSample
	{
		public uint Time;

		public long HP;
	}

	private struct EleSettings
	{
		public bool UseFireElemental;

		public bool UseFlameShock;

		public bool UseLavaBurst;

		public bool UseLightningBolt;

		public bool UseChainLightning;

		public bool UseEarthShock;

		public bool UseFrostShock;

		public bool UseThunderstorm;

		public bool UseElementalMastery;

		public bool ChainLightningEnable;

		public bool ChainLightningAoe;

		public bool RingOfFireEnable;

		public int RingOfFireTargets;

		public bool ThunderstormAoe;

		public int ThunderstormMana;

		public bool UseEngGloves;

		public bool UseRacial;

		public bool UseTrinket1;

		public bool UseTrinket2;

		public string ActiveFireTotem;

		public string ActiveWaterTotem;

		public string ActiveEarthTotem;

		public string ActiveAirTotem;

		public string ActiveShield;

		public string ActiveWeapon;
	}

	private struct RestoSettings
	{
		public bool UseRiptide;

		public bool UseHealingWave;

		public bool UseChainHeal;

		public bool LesserHealingWaveEnable;

		public bool LowManaLhwEnable;

		public bool UseManaTide;

		public int ManaTideTotemPercent;

		public bool UseNaturesSwiftness;

		public bool HealOutOfCombat;

		public bool EarthShieldRefresh;

		public bool EarthShieldFocus;

		public int EarthShieldRefreshThresholdMs;

		public string EarthShieldName;

		public bool ValithriaEnable;

		public bool RiptideTank;

		public int AllowedOverhealPct;

		public bool UseEngGloves;

		public bool UseRacial;

		public bool UseTrinket1;

		public bool UseTrinket2;

		public string ActiveFireTotem;

		public string ActiveWaterTotem;

		public string ActiveEarthTotem;

		public string ActiveAirTotem;

		public string ActiveShield;

		public string ActiveWeapon;
	}

	private struct CommonSettings
	{
		public string SelectedSpec;

		public bool UseWindShear;

		public bool UsePurge;

		public bool UseCleanseSpirit;

		public bool UseCureDisease;

		public int TotemAliveTime;

		public bool TotemRecall;

		public bool EnableRegen;

		public int HpRegenPct;

		public int MpRegenPct;

		public bool UseSaroniteBomb;

		public bool UseThermalSapper;

		public bool UseFelHealthstone;

		public bool UseRunicHealingPotion;

		public bool UseRunicManaPotion;

		public bool UsePotionSpeedCombat;

		public bool UsePotionSpeedPrepot;

		public bool UsePotionWildMagicCombat;

		public bool UsePotionWildMagicPrepot;

		public string DbmBars;

		public bool UseFlaskDistilledWisdom;

		public bool UseFlaskToughness;

		public bool UseFlaskResistance;

		public bool UseElixirAgility;

		public bool UseElixirMightyMageblood;

		public bool UseElixirMightySpirit;

		public bool UseElixirMightyThoughts;

		public bool UseElixirOgreStr;

		public bool UseElixirMightyDefense;

		public bool UseFlaskEndlessRage;

		public bool UseFlaskFrostWyrm;

		public bool UseFlaskNorth;

		public bool UseFlaskPureMojo;

		public bool UseFlaskStoneblood;

		public bool UseElixirArmorPierce;

		public bool UseElixirDeadlyStrikes;

		public bool UseElixirExpertise;

		public bool UseElixirGreaterStr;

		public bool UseElixirMightyStr;

		public bool UseElixirLightningSpeed;

		public bool UseElixirMastery;

		public bool UseElixirSpellpower;
	}

	private struct ConfigCache
	{
		public EleSettings Ele;

		public RestoSettings Resto;

		public CommonSettings Common;
	}

	private enum PanicState
	{
		None,
		CastNS,
		CastTidal,
		CastHW
	}

	private enum TotemPresetState
	{
		Dirty,
		Synced,
		ReadyToCall,
		Called,
		Verified
	}

	private const bool FULL_TRACE = true;

	private ShamanCapabilities _caps;

	private long _fireEleWaitStart = 0L;

	private long _traceTick = 0L;

	private static uint _lastSyncEarth = 0u;

	private static uint _lastSyncFire = 0u;

	private static uint _lastSyncWater = 0u;

	private static uint _lastSyncAir = 0u;

	private volatile bool _isLaunched;

	private static bool _presetDirty = true;

	private static bool _presetAppliedInCombat = false;

	private Thread _configThread;

	private Thread _rotationThread;

	private static object _cacheLock = new object();

	private uint _lastCatchLog = 0u;

	private uint _lastConfigReadyLog = 0u;

	private static Dictionary<ulong, List<HealthSample>> _healthHistory = new Dictionary<ulong, List<HealthSample>>();

	private static ConfigCache _config = new ConfigCache
	{
		Resto = new RestoSettings
		{
			AllowedOverhealPct = 25,
			LowManaLhwEnable = false,
			LesserHealingWaveEnable = true,
			RiptideTank = true,
			HealOutOfCombat = true,
			ManaTideTotemPercent = 60
		},
		Common = new CommonSettings
		{
			SelectedSpec = "Restoration",
			DbmBars = ""
		}
	};

	private static Dictionary<string, uint> _expectedStates = new Dictionary<string, uint>();

	private static readonly object _stateLock = new object();

	private string _lastWeaponEnchant = "";

	private bool _resolverLogged = false;

	private static readonly Dictionary<string, uint[]> SpellRanks = new Dictionary<string, uint[]>
	{
		{
			"call_of_the_elements",
			new uint[1] { 66842u }
		},
		{
			"fire_nova",
			new uint[1] { 61657u }
		},
		{
			"fire_resistance_totem",
			new uint[6] { 58739u, 58737u, 25563u, 10538u, 10537u, 8184u }
		},
		{
			"flame_shock",
			new uint[9] { 49233u, 29228u, 25457u, 10448u, 10447u, 8050u, 8049u, 8047u, 8042u }
		},
		{
			"lava_burst",
			new uint[2] { 60043u, 51505u }
		},
		{
			"lightning_bolt",
			new uint[10] { 49238u, 25449u, 15208u, 10392u, 10391u, 10390u, 8246u, 915u, 529u, 403u }
		},
		{
			"chain_lightning",
			new uint[6] { 49271u, 25442u, 15117u, 10605u, 2860u, 421u }
		},
		{
			"earth_shock",
			new uint[6] { 49231u, 25454u, 10413u, 10412u, 10411u, 8042u }
		},
		{
			"frost_shock",
			new uint[5] { 49236u, 25464u, 10473u, 8056u, 8055u }
		},
		{
			"thunderstorm",
			new uint[2] { 59159u, 51490u }
		},
		{
			"ring_of_fire",
			new uint[1] { 61657u }
		},
		{
			"water_shield",
			new uint[3] { 57960u, 24398u, 33736u }
		},
		{
			"lightning_shield",
			new uint[9] { 49281u, 49280u, 25472u, 10432u, 10431u, 8012u, 8011u, 325u, 324u }
		},
		{
			"earthliving_weapon",
			new uint[6] { 51994u, 51993u, 51992u, 51991u, 51988u, 51730u }
		},
		{
			"flametongue_weapon",
			new uint[10] { 58790u, 58789u, 58788u, 25489u, 16342u, 16341u, 10400u, 8027u, 8024u, 8023u }
		},
		{
			"windfury_weapon",
			new uint[9] { 58804u, 58803u, 58801u, 25505u, 16362u, 16344u, 16343u, 8235u, 8232u }
		},
		{
			"frostbrand_weapon",
			new uint[9] { 58796u, 58795u, 58794u, 25500u, 16356u, 16355u, 10457u, 10456u, 8033u }
		},
		{
			"rockbiter_weapon",
			new uint[7] { 8017u, 8019u, 8018u, 10399u, 16316u, 25485u, 58785u }
		},
		{
			"magma_totem",
			new uint[8] { 58734u, 58731u, 25552u, 15271u, 10587u, 10586u, 10585u, 8190u }
		},
		{
			"flametongue_totem",
			new uint[7] { 58656u, 58652u, 25557u, 16387u, 10526u, 8247u, 8227u }
		},
		{
			"searing_totem",
			new uint[10] { 58704u, 58699u, 25533u, 10584u, 10583u, 10582u, 8181u, 8180u, 6364u, 6363u }
		},
		{
			"totem_of_wrath",
			new uint[4] { 57722u, 57721u, 57720u, 30706u }
		},
		{
			"frost_resistance_totem",
			new uint[6] { 58745u, 58741u, 25560u, 10479u, 10478u, 8181u }
		},
		{
			"mana_spring_totem",
			new uint[9] { 58774u, 58773u, 58771u, 25570u, 10497u, 10496u, 10495u, 8170u, 5675u }
		},
		{
			"healing_stream_totem",
			new uint[9] { 58757u, 58756u, 58755u, 25567u, 10463u, 10462u, 6377u, 6375u, 5394u }
		},
		{
			"cleansing_totem",
			new uint[1] { 8170u }
		},
		{
			"fire_resistance_totem_water",
			new uint[6] { 58739u, 58737u, 25563u, 10538u, 10537u, 8181u }
		},
		{
			"earthbind_totem",
			new uint[1] { 2484u }
		},
		{
			"tremor_totem",
			new uint[1] { 8143u }
		},
		{
			"stoneskin_totem",
			new uint[9] { 58753u, 58751u, 25509u, 10408u, 10407u, 10406u, 8155u, 8154u, 8071u }
		},
		{
			"strength_of_earth_totem",
			new uint[5] { 58643u, 25528u, 10442u, 8161u, 8160u }
		},
		{
			"stoneclaw_totem",
			new uint[10] { 58582u, 58581u, 58580u, 25525u, 10428u, 10427u, 8146u, 6392u, 6391u, 6390u }
		},
		{
			"grounding_totem",
			new uint[1] { 8177u }
		},
		{
			"windfury_totem",
			new uint[5] { 8512u, 10613u, 10614u, 25585u, 25587u }
		},
		{
			"wrath_of_air_totem",
			new uint[1] { 3738u }
		},
		{
			"nature_resistance_totem",
			new uint[6] { 58749u, 58746u, 25574u, 10601u, 10600u, 10595u }
		},
		{
			"elemental_mastery",
			new uint[1] { 16166u }
		},
		{
			"blood_fury",
			new uint[5] { 33697u, 20572u, 33702u, 33698u, 24516u }
		},
		{
			"berserking",
			new uint[1] { 26297u }
		},
		{
			"purge",
			new uint[2] { 8012u, 8011u }
		},
		{
			"totemic_recall",
			new uint[1] { 36936u }
		},
		{
			"healing_wave",
			new uint[7] { 49273u, 25357u, 10627u, 10395u, 939u, 547u, 331u }
		},
		{
			"lesser_healing_wave",
			new uint[6] { 49276u, 25420u, 10468u, 8005u, 8004u, 8008u }
		},
		{
			"chain_heal",
			new uint[4] { 55459u, 25423u, 10623u, 1064u }
		},
		{
			"riptide",
			new uint[6] { 61301u, 61295u, 61299u, 61300u, 55340u, 55339u }
		},
		{
			"earth_shield",
			new uint[3] { 49284u, 32593u, 32594u }
		},
		{
			"mana_tide_totem",
			new uint[3] { 16190u, 16191u, 17359u }
		},
		{
			"wind_shear",
			new uint[1] { 57994u }
		},
		{
			"cleanse_spirit",
			new uint[1] { 51886u }
		},
		{
			"cure_disease",
			new uint[1] { 526u }
		},
		{
			"natures_swiftness",
			new uint[1] { 16188u }
		},
		{
			"tidal_force",
			new uint[1] { 55198u }
		},
		{
			"fire_elemental_totem",
			new uint[1] { 2894u }
		}
	};

	private static PanicState _panicState = PanicState.None;

	private static ulong _panicTargetGuid = 0uL;

	private static uint _panicDeadline = 0u;

	private static int _callFailCount = 0;
	private static TotemPresetState _totemState = TotemPresetState.Dirty;
    private static TotemRestoreAction _lastRestoreAction = TotemRestoreAction.NONE;
    private static bool _lastDpsPolicyAllow = false;
    private static long _fsmTickId = 0;
    private static long _policyTickId = -1;
    private static bool _policyValid = false;
    private static bool _lastBaseVerified = false;
    private static bool _lastOverrideActive = false;
    private static bool _lastSpecialDpsActive = false;
    private static string _lastDpsReason = "";

	private static uint _totemVerifyTime = 0u;

	public float Range { get { return 30f; } }

	private static System.IO.StreamWriter _traceWriter;
    private void FT(string message)
    {
        Logging.Write("[FULL TRACE][" + ++_traceTick + "][" + Environment.TickCount + "] " + message);
        try {
            if (_traceWriter == null)
            {
                string logPath = System.IO.Path.Combine(System.Windows.Forms.Application.StartupPath, "Logs", DateTime.Now.ToString("dd MMM yyyy HH'H'mm") + ".log.html");
                _traceWriter = new System.IO.StreamWriter(logPath, false, System.Text.Encoding.UTF8);
                _traceWriter.AutoFlush = true;
                _traceWriter.WriteLine("<html><body style='font-family:monospace; background-color:#1e1e1e; color:#d4d4d4;'>");
            }
            _traceWriter.WriteLine("<div><b style='color:#569cd6'>[" + _traceTick + "]</b> <span style='color:#ce9178'>" + message + "</span></div>");
        } catch {}
    }

    private void FTLine(string message)
    {
        Logging.Write("[FULL TRACE]    " + message);
        try {
            if (_traceWriter != null)
            {
                _traceWriter.WriteLine("<div style='margin-left: 20px; color:#dcdcaa'>" + message + "</div>");
            }
        } catch {}
    }

	private void FTResult(string state, bool result)
	{
		bool flag = true;
		Logging.Write("[FULL TRACE]    RESULT " + state + " = " + (result ? "TRUE -> RETURN" : "FALSE -> CONTINUE"));
	}

	private string FTUnit(WoWUnit u)
	{
		if (u == null)
		{
			return "NULL";
		}
		try
		{
			return "name=" + ((WoWObject)u).Name + " guid=" + ((WoWObject)u).Guid + " valid=" + ((WoWObject)u).IsValid + " alive=" + u.IsAlive + " attackable=" + u.IsAttackable + " dist=" + ((WoWObject)u).GetDistance.ToString("F1") + " hp=" + u.HealthPercent;
		}
		catch
		{
			return "UNIT_INFO_ERROR";
		}
	}

	private void FTStateStart(string state)
	{
		bool flag = true;
		Logging.Write("[FULL TRACE] >>> ENTER " + state);
	}

	private void FTStateEnd(string state)
	{
		bool flag = true;
		Logging.Write("[FULL TRACE] <<< EXIT " + state);
	}

	private void TrackHealth(List<WoWUnit> validTargets)
	{
		uint tickCount = (uint)Environment.TickCount;
		foreach (WoWUnit validTarget in validTargets)
		{
			if (!_healthHistory.ContainsKey(((WoWObject)validTarget).Guid))
			{
				_healthHistory[((WoWObject)validTarget).Guid] = new List<HealthSample>();
			}
			List<HealthSample> list = _healthHistory[((WoWObject)validTarget).Guid];
			list.Add(new HealthSample
			{
				Time = tickCount,
				HP = validTarget.Health
			});
			while (list.Count > 0 && tickCount - list[0].Time > 3000)
			{
				list.RemoveAt(0);
			}
		}
		List<ulong> list2 = new List<ulong>(_healthHistory.Keys);
		foreach (ulong item in list2)
		{
			List<HealthSample> list3 = _healthHistory[item];
			if (list3.Count == 0 || tickCount - list3[list3.Count - 1].Time > 10000)
			{
				_healthHistory.Remove(item);
			}
		}
	}

	private float GetRecentDamageRate(ulong guid)
	{
		if (!_healthHistory.ContainsKey(guid))
		{
			return 0f;
		}
		List<HealthSample> list = _healthHistory[guid];
		if (list.Count < 2)
		{
			return 0f;
		}
		uint tickCount = (uint)Environment.TickCount;
		float num = 0f;
		float num2 = 0f;
		for (int i = 1; i < list.Count; i++)
		{
			HealthSample healthSample = list[i - 1];
			HealthSample healthSample2 = list[i];
			float num3 = (float)healthSample.HP - (float)healthSample2.HP;
			if (num3 <= 0f)
			{
				continue;
			}
			float num4 = (float)(healthSample2.Time - healthSample.Time) / 1000f;
			if (!(num4 <= 0.001f))
			{
				float num5 = num3 / num4;
				uint num6 = tickCount - healthSample2.Time;
				float num7 = 0f;
				if (num6 <= 500)
				{
					num7 = 1f;
				}
				else if (num6 <= 1500)
				{
					num7 = 0.7f;
				}
				else if (num6 <= 3000)
				{
					num7 = 0.3f;
				}
				num += num5 * num7;
				num2 += num7;
			}
		}
		if (num2 <= 0f)
		{
			return 0f;
		}
		return num / num2;
	}

	
	

	
	

	
	

	
	

	
	

	
	

	public void Initialize()
	{
		FT("============================================================");
		FT("RAW FULL TRACE SESSION START");
		FT("============================================================");
		
		
		
		
		
		
		_isLaunched = true;
		_configThread = new Thread(ConfigLoop);
		_configThread.Priority = ThreadPriority.Lowest;
		_configThread.Start();
		_rotationThread = new Thread(RotationLoop);
		_rotationThread.Start();
		Logging.Write("[Shaman PVE] FSM Loaded.");
	}

	public void Dispose()
	{
		FT("============================================================");
		FT("RAW FULL TRACE SESSION END");
		FT("============================================================");
		_isLaunched = false;
		if (_configThread != null && _configThread.IsAlive)
		{
			_configThread.Join(500);
		}
		if (_rotationThread != null && _rotationThread.IsAlive)
		{
			_rotationThread.Join(500);
		}
	}

	public void ShowConfiguration()
	{
	}

	private void ConfigLoop()
	{
		while (_isLaunched)
		{
			try
			{
				if (Conditions.InGameAndConnectedAndAlive)
				{
					string text = "\n                        local function GetB(name)\n                            local val = _G['WRobot_Setting_Shaman_PVE.cs_' .. name]\n                            if val == nil then val = _G['WRobot_Skill_Shaman_PVE.cs_' .. name] end\n                            if val == nil then val = _G['WRobot_Skill_Shaman_PVE.cs_' .. name:gsub('use_', '')] end\n                            if val == true or tostring(val) == '1' or tostring(val) == 'true' then return '1' else return '0' end\n                        end\n                        local function GetN(name)\n                            local val = _G['WRobot_Setting_Shaman_PVE.cs_' .. name]\n                            if val == nil then return '0' end\n                            return tostring(val)\n                        end\n                        local function GetS_Val(name)\n                            local val = _G['WRobot_Setting_Shaman_PVE.cs_' .. name]\n                            if val == nil then return '' end\n                            return tostring(val)\n                        end\n                        local function GetS(name)\n                            local val = _G['WRobot_Skill_Shaman_PVE.cs_' .. name]\n                            if val == true or tostring(val) == '1' or tostring(val) == 'true' then return '1' else return '0' end\n                        end\n                        \n                        local spec = _G['WRobot_Global_SelectedSpec']\n                        if spec == nil or spec == '' then spec = 'DISABLED' end\n                        \n                        local out = ''\n                        local dbmRet = ''\n                        if DBT then\n                            for bar in DBT:GetBarIterator() do\n                                if bar.timer and bar.timer < 3.5 then\n                                    dbmRet = dbmRet .. tostring(bar.id) .. ':' .. tostring(bar.timer) .. '^'\n                                end\n                            end\n                        end\n                        \n                                                \n                        local function GetSkill(spec, name)\n                            local val = _G['WRobot_Skill_Shaman_PVE.cs_' .. spec .. '_' .. name]\n                            if val == true or tostring(val) == '1' or tostring(val) == 'true' then return '1' else return '0' end\n                        end\n                        \n                        -- ELE\n                        local ele_sld = ''\n                        if GetSkill('ele', 'water_shield') == '1' then ele_sld = 'water_shield'\n                        elseif GetSkill('ele', 'lightning_shield') == '1' then ele_sld = 'lightning_shield' end\n                        \n                        local ele_wep = ''\n                        if GetSkill('ele', 'earthliving_weapon') == '1' then ele_wep = 'earthliving_weapon'\n                        elseif GetSkill('ele', 'flametongue_weapon') == '1' then ele_wep = 'flametongue_weapon'\n                        elseif GetSkill('ele', 'windfury_weapon') == '1' then ele_wep = 'windfury_weapon'\n                        elseif GetSkill('ele', 'frostbrand_weapon') == '1' then ele_wep = 'frostbrand_weapon'\n                        elseif GetSkill('ele', 'rockbiter_weapon') == '1' then ele_wep = 'rockbiter_weapon' end\n                        \n                                                                        local ele_tF = GetS_Val('ele_selected_fire_totem')\n                        if ele_tF == '' then\n                        ele_tF = ''\n                        if GetSkill('ele', 'magma_totem') == '1' then ele_tF = 'magma_totem'\n                        elseif GetSkill('ele', 'flametongue_totem') == '1' then ele_tF = 'flametongue_totem'\n                        elseif GetSkill('ele', 'searing_totem') == '1' then ele_tF = 'searing_totem'\n                        elseif GetSkill('ele', 'totem_of_wrath') == '1' then ele_tF = 'totem_of_wrath'\n                        elseif GetSkill('ele', 'frost_resistance_totem') == '1' then ele_tF = 'frost_resistance_totem' end\n                        end\n                        \n                        local ele_tW = GetS_Val('ele_selected_water_totem')\n                        if ele_tW == '' then\n                        ele_tW = ''\n                        if GetSkill('ele', 'mana_spring_totem') == '1' then ele_tW = 'mana_spring_totem'\n                        elseif GetSkill('ele', 'healing_stream_totem') == '1' then ele_tW = 'healing_stream_totem'\n                        elseif GetSkill('ele', 'cleansing_totem') == '1' then ele_tW = 'cleansing_totem'\n                        elseif GetSkill('ele', 'fire_resistance_totem_water') == '1' then ele_tW = 'fire_resistance_totem_water' end\n                        end\n                        \n                        local ele_tE = GetS_Val('ele_selected_earth_totem')\n                        if ele_tE == '' then\n                        ele_tE = ''\n                        if GetSkill('ele', 'earthbind_totem') == '1' then ele_tE = 'earthbind_totem'\n                        elseif GetSkill('ele', 'tremor_totem') == '1' then ele_tE = 'tremor_totem'\n                        elseif GetSkill('ele', 'stoneskin_totem') == '1' then ele_tE = 'stoneskin_totem'\n                        elseif GetSkill('ele', 'strength_of_earth_totem') == '1' then ele_tE = 'strength_of_earth_totem' end\n                        end\n                        \n                        local ele_tA = GetS_Val('ele_selected_air_totem')\n                        if ele_tA == '' then\n                        ele_tA = ''\n                        if GetSkill('ele', 'grounding_totem') == '1' then ele_tA = 'grounding_totem'\n                        elseif GetSkill('ele', 'windfury_totem') == '1' then ele_tA = 'windfury_totem'\n                        elseif GetSkill('ele', 'wrath_of_air_totem') == '1' then ele_tA = 'wrath_of_air_totem'\n                        elseif GetSkill('ele', 'nature_resistance_totem') == '1' then ele_tA = 'nature_resistance_totem' end\n                        end\n\n                        -- RESTO\n                        local resto_sld = ''\n                        if GetSkill('resto', 'water_shield') == '1' then resto_sld = 'water_shield'\n                        elseif GetSkill('resto', 'lightning_shield') == '1' then resto_sld = 'lightning_shield' end\n                        \n                        local resto_wep = ''\n                        if GetSkill('resto', 'earthliving_weapon') == '1' then resto_wep = 'earthliving_weapon'\n                        elseif GetSkill('resto', 'flametongue_weapon') == '1' then resto_wep = 'flametongue_weapon'\n                        elseif GetSkill('resto', 'windfury_weapon') == '1' then resto_wep = 'windfury_weapon'\n                        elseif GetSkill('resto', 'frostbrand_weapon') == '1' then resto_wep = 'frostbrand_weapon'\n                        elseif GetSkill('resto', 'rockbiter_weapon') == '1' then resto_wep = 'rockbiter_weapon' end\n                        \n                        local resto_tF = GetS_Val('resto_selected_fire_totem')\n                        if resto_tF == '' then\n                        resto_tF = ''\n                        if GetSkill('resto', 'magma_totem') == '1' then resto_tF = 'magma_totem'\n                        elseif GetSkill('resto', 'flametongue_totem') == '1' then resto_tF = 'flametongue_totem'\n                        elseif GetSkill('resto', 'searing_totem') == '1' then resto_tF = 'searing_totem'\n                        elseif GetSkill('resto', 'totem_of_wrath') == '1' then resto_tF = 'totem_of_wrath'\n                        elseif GetSkill('resto', 'frost_resistance_totem') == '1' then resto_tF = 'frost_resistance_totem' end\n                        end\n                        \n                        local resto_tW = GetS_Val('resto_selected_water_totem')\n                        if resto_tW == '' then\n                        resto_tW = ''\n                        if GetSkill('resto', 'mana_spring_totem') == '1' then resto_tW = 'mana_spring_totem'\n                        elseif GetSkill('resto', 'healing_stream_totem') == '1' then resto_tW = 'healing_stream_totem'\n                        elseif GetSkill('resto', 'cleansing_totem') == '1' then resto_tW = 'cleansing_totem'\n                        elseif GetSkill('resto', 'fire_resistance_totem_water') == '1' then resto_tW = 'fire_resistance_totem_water' end\n                        end\n                        \n                        local resto_tE = GetS_Val('resto_selected_earth_totem')\n                        if resto_tE == '' then\n                        resto_tE = ''\n                        if GetSkill('resto', 'earthbind_totem') == '1' then resto_tE = 'earthbind_totem'\n                        elseif GetSkill('resto', 'tremor_totem') == '1' then resto_tE = 'tremor_totem'\n                        elseif GetSkill('resto', 'stoneskin_totem') == '1' then resto_tE = 'stoneskin_totem'\n                        elseif GetSkill('resto', 'strength_of_earth_totem') == '1' then resto_tE = 'strength_of_earth_totem' end\n                        end\n                        \n                        local resto_tA = GetS_Val('resto_selected_air_totem')\n                        if resto_tA == '' then\n                        resto_tA = ''\n                        if GetSkill('resto', 'grounding_totem') == '1' then resto_tA = 'grounding_totem'\n                        elseif GetSkill('resto', 'windfury_totem') == '1' then resto_tA = 'windfury_totem'\n                        elseif GetSkill('resto', 'wrath_of_air_totem') == '1' then resto_tA = 'wrath_of_air_totem'\n                        elseif GetSkill('resto', 'nature_resistance_totem') == '1' then resto_tA = 'nature_resistance_totem' end\n                        end\n\n                        out = out .. 'ele_use_fire_elemental=' .. GetB('ele_use_fire_elemental') .. '|'\nout = out .. 'ele_use_flame_shock=' .. GetB('ele_use_flame_shock') .. '|'\nout = out .. 'ele_use_lava_burst=' .. GetB('ele_use_lava_burst') .. '|'\nout = out .. 'ele_use_lightning_bolt=' .. GetB('ele_use_lightning_bolt') .. '|'\nout = out .. 'ele_use_chain_lightning=' .. GetB('ele_use_chain_lightning') .. '|'\nout = out .. 'ele_use_earth_shock=' .. GetB('ele_use_earth_shock') .. '|'\nout = out .. 'ele_use_frost_shock=' .. GetB('ele_use_frost_shock') .. '|'\nout = out .. 'ele_use_thunderstorm=' .. GetB('ele_use_thunderstorm') .. '|'\nout = out .. 'ele_use_elemental_mastery=' .. GetB('ele_use_elemental_mastery') .. '|'\nout = out .. 'ele_chain_lightning_enable=' .. GetB('ele_chain_lightning_enable') .. '|'\nout = out .. 'ele_chain_lightning_aoe=' .. GetB('ele_chain_lightning_aoe') .. '|'\nout = out .. 'ele_ring_of_fire_enable=' .. GetB('ele_ring_of_fire_enable') .. '|'\nout = out .. 'ele_ring_of_fire_targets=' .. GetN('ele_ring_of_fire_targets') .. '|'\nout = out .. 'ele_thunderstorm_aoe=' .. GetB('ele_thunderstorm_aoe') .. '|'\nout = out .. 'ele_thunderstorm_mana=' .. GetN('ele_thunderstorm_mana') .. '|'\nout = out .. 'ele_use_eng_gloves=' .. GetB('ele_use_eng_gloves') .. '|'\nout = out .. 'ele_use_racial=' .. GetB('ele_use_racial') .. '|'\nout = out .. 'ele_use_trinket1=' .. GetB('ele_use_trinket1') .. '|'\nout = out .. 'ele_use_trinket2=' .. GetB('ele_use_trinket2') .. '|'\nout = out .. 'ele_active_fire_totem=' .. ele_tF .. '|'\nout = out .. 'ele_active_water_totem=' .. ele_tW .. '|'\nout = out .. 'ele_active_earth_totem=' .. ele_tE .. '|'\nout = out .. 'ele_active_air_totem=' .. ele_tA .. '|'\nout = out .. 'ele_active_shield=' .. ele_sld .. '|'\nout = out .. 'ele_active_weapon=' .. ele_wep .. '|'\nout = out .. 'resto_use_riptide=' .. GetB('resto_use_riptide') .. '|'\nout = out .. 'resto_use_healing_wave=' .. GetB('resto_use_healing_wave') .. '|'\nout = out .. 'resto_use_chain_heal=' .. GetB('resto_use_chain_heal') .. '|'\nout = out .. 'resto_lesser_healing_wave_enable=' .. GetB('resto_lesser_healing_wave_enable') .. '|'\nout = out .. 'resto_low_mana_lhw_enable=' .. GetB('resto_low_mana_lhw_enable') .. '|'\nout = out .. 'resto_use_mana_tide=' .. GetB('resto_use_mana_tide') .. '|'\nout = out .. 'resto_mana_tide_totem_percent=' .. GetN('resto_mana_tide_totem_percent') .. '|'\nout = out .. 'resto_use_natures_swiftness=' .. GetB('resto_use_natures_swiftness') .. '|'\nout = out .. 'resto_heal_out_of_combat=' .. GetB('resto_heal_out_of_combat') .. '|'\nout = out .. 'resto_earth_shield_refresh=' .. GetB('resto_earth_shield_refresh') .. '|'\nout = out .. 'resto_earth_shield_focus=' .. GetB('resto_earth_shield_focus') .. '|'\nout = out .. 'resto_earth_shield_name=' .. GetS_Val('resto_earth_shield_name') .. '|'\nout = out .. 'resto_valithria_enable=' .. GetB('resto_valithria_enable') .. '|'\nout = out .. 'resto_riptide_tank=' .. GetB('resto_riptide_tank') .. '|'\nout = out .. 'resto_overheal_percent=' .. GetN('resto_overheal_percent') .. '|'\nout = out .. 'resto_use_eng_gloves=' .. GetB('resto_use_eng_gloves') .. '|'\nout = out .. 'resto_use_racial=' .. GetB('resto_use_racial') .. '|'\nout = out .. 'resto_use_trinket1=' .. GetB('resto_use_trinket1') .. '|'\nout = out .. 'resto_use_trinket2=' .. GetB('resto_use_trinket2') .. '|'\nout = out .. 'resto_active_fire_totem=' .. resto_tF .. '|'\nout = out .. 'resto_active_water_totem=' .. resto_tW .. '|'\nout = out .. 'resto_active_earth_totem=' .. resto_tE .. '|'\nout = out .. 'resto_active_air_totem=' .. resto_tA .. '|'\nout = out .. 'resto_active_shield=' .. resto_sld .. '|'\nout = out .. 'resto_active_weapon=' .. resto_wep .. '|'\nout = out .. 'common_use_wind_shear=' .. GetB('common_use_wind_shear') .. '|'\nout = out .. 'common_use_purge=' .. GetB('common_use_purge') .. '|'\nout = out .. 'common_use_cleanse_spirit=' .. GetB('common_use_cleanse_spirit') .. '|'\nout = out .. 'common_use_cure_disease=' .. GetB('common_use_cure_disease') .. '|'\nout = out .. 'common_totem_alive_time=' .. GetN('common_totem_alive_time') .. '|'\nout = out .. 'common_totem_recall=' .. GetB('common_totem_recall') .. '|'\nout = out .. 'common_enable_regen=' .. GetB('common_enable_regen') .. '|'\nout = out .. 'common_hp_regen_pct=' .. GetN('common_hp_regen_pct') .. '|'\nout = out .. 'common_mp_regen_pct=' .. GetN('common_mp_regen_pct') .. '|'\nout = out .. 'common_use_saronite_bomb=' .. GetB('common_use_saronite_bomb') .. '|'\nout = out .. 'common_use_thermal_sapper=' .. GetB('common_use_thermal_sapper') .. '|'\nout = out .. 'common_use_fel_healthstone=' .. GetB('common_use_fel_healthstone') .. '|'\nout = out .. 'common_use_runic_healing_potion=' .. GetB('common_use_runic_healing_potion') .. '|'\nout = out .. 'common_use_runic_mana_potion=' .. GetB('common_use_runic_mana_potion') .. '|'\nout = out .. 'common_use_potion_speed_combat=' .. GetB('common_use_potion_speed_combat') .. '|'\nout = out .. 'common_use_potion_speed_prepot=' .. GetB('common_use_potion_speed_prepot') .. '|'\nout = out .. 'common_use_potion_wild_magic_combat=' .. GetB('common_use_potion_wild_magic_combat') .. '|'\nout = out .. 'common_use_potion_wild_magic_prepot=' .. GetB('common_use_potion_wild_magic_prepot') .. '|'\nout = out .. 'common_use_flask_distilled_wisdom=' .. GetB('common_use_flask_distilled_wisdom') .. '|'\nout = out .. 'common_use_flask_toughness=' .. GetB('common_use_flask_toughness') .. '|'\nout = out .. 'common_use_flask_resistance=' .. GetB('common_use_flask_resistance') .. '|'\nout = out .. 'common_use_elixir_agility=' .. GetB('common_use_elixir_agility') .. '|'\nout = out .. 'common_use_elixir_mighty_mageblood=' .. GetB('common_use_elixir_mighty_mageblood') .. '|'\nout = out .. 'common_use_elixir_mighty_spirit=' .. GetB('common_use_elixir_mighty_spirit') .. '|'\nout = out .. 'common_use_elixir_mighty_thoughts=' .. GetB('common_use_elixir_mighty_thoughts') .. '|'\nout = out .. 'common_use_elixir_ogre_str=' .. GetB('common_use_elixir_ogre_str') .. '|'\nout = out .. 'common_use_elixir_mighty_defense=' .. GetB('common_use_elixir_mighty_defense') .. '|'\nout = out .. 'common_use_flask_endless_rage=' .. GetB('common_use_flask_endless_rage') .. '|'\nout = out .. 'common_use_flask_frost_wyrm=' .. GetB('common_use_flask_frost_wyrm') .. '|'\nout = out .. 'common_use_flask_north=' .. GetB('common_use_flask_north') .. '|'\nout = out .. 'common_use_flask_pure_mojo=' .. GetB('common_use_flask_pure_mojo') .. '|'\nout = out .. 'common_use_flask_stoneblood=' .. GetB('common_use_flask_stoneblood') .. '|'\nout = out .. 'common_use_elixir_armor_pierce=' .. GetB('common_use_elixir_armor_pierce') .. '|'\nout = out .. 'common_use_elixir_deadly_strikes=' .. GetB('common_use_elixir_deadly_strikes') .. '|'\nout = out .. 'common_use_elixir_expertise=' .. GetB('common_use_elixir_expertise') .. '|'\nout = out .. 'common_use_elixir_greater_str=' .. GetB('common_use_elixir_greater_str') .. '|'\nout = out .. 'common_use_elixir_mighty_str=' .. GetB('common_use_elixir_mighty_str') .. '|'\nout = out .. 'common_use_elixir_lightning_speed=' .. GetB('common_use_elixir_lightning_speed') .. '|'\nout = out .. 'common_use_elixir_mastery=' .. GetB('common_use_elixir_mastery') .. '|'\nout = out .. 'common_use_elixir_spellpower=' .. GetB('common_use_elixir_spellpower') .. '|'\nout = out .. 'common_selected_spec=' .. spec .. '|'\nout = out .. 'common_dbm_bars=' .. dbmRet .. '|'\n\n                        return out;";
					string text2 = Lua.LuaDoString<string>(text, "");
					if (string.IsNullOrEmpty(text2))
					{
						Thread.Sleep(500);
						continue;
					}
					string[] array = text2.Split(new char[1] { '|' }, StringSplitOptions.RemoveEmptyEntries);
					bool flag = false;
					ConfigCache config = default(ConfigCache);
					HashSet<string> hashSet = new HashSet<string>();
					string[] array2 = array;
					string[] array3 = array2;
					foreach (string text3 in array3)
					{
						if (string.IsNullOrEmpty(text3))
						{
							flag = true;
							break;
						}
						string[] array4 = text3.Split(new char[1] { '=' }, 2);
						if (array4.Length == 2)
						{
							string text4 = array4[0];
							string text5 = array4[1];
							if (hashSet.Contains(text4))
							{
								flag = true;
								break;
							}
							hashSet.Add(text4);
							int result;
							switch (text4)
							{
							case "ele_use_fire_elemental":
								if (text5 == "1")
								{
									config.Ele.UseFireElemental = true;
								}
								else if (text5 == "0")
								{
									config.Ele.UseFireElemental = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "ele_use_flame_shock":
								if (text5 == "1")
								{
									config.Ele.UseFlameShock = true;
								}
								else if (text5 == "0")
								{
									config.Ele.UseFlameShock = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "ele_use_lava_burst":
								if (text5 == "1")
								{
									config.Ele.UseLavaBurst = true;
								}
								else if (text5 == "0")
								{
									config.Ele.UseLavaBurst = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "ele_use_lightning_bolt":
								if (text5 == "1")
								{
									config.Ele.UseLightningBolt = true;
								}
								else if (text5 == "0")
								{
									config.Ele.UseLightningBolt = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "ele_use_chain_lightning":
								if (text5 == "1")
								{
									config.Ele.UseChainLightning = true;
								}
								else if (text5 == "0")
								{
									config.Ele.UseChainLightning = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "ele_use_earth_shock":
								if (text5 == "1")
								{
									config.Ele.UseEarthShock = true;
								}
								else if (text5 == "0")
								{
									config.Ele.UseEarthShock = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "ele_use_frost_shock":
								if (text5 == "1")
								{
									config.Ele.UseFrostShock = true;
								}
								else if (text5 == "0")
								{
									config.Ele.UseFrostShock = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "ele_use_thunderstorm":
								if (text5 == "1")
								{
									config.Ele.UseThunderstorm = true;
								}
								else if (text5 == "0")
								{
									config.Ele.UseThunderstorm = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "ele_use_elemental_mastery":
								if (text5 == "1")
								{
									config.Ele.UseElementalMastery = true;
								}
								else if (text5 == "0")
								{
									config.Ele.UseElementalMastery = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "ele_chain_lightning_enable":
								if (text5 == "1")
								{
									config.Ele.ChainLightningEnable = true;
								}
								else if (text5 == "0")
								{
									config.Ele.ChainLightningEnable = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "ele_chain_lightning_aoe":
								if (text5 == "1")
								{
									config.Ele.ChainLightningAoe = true;
								}
								else if (text5 == "0")
								{
									config.Ele.ChainLightningAoe = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "ele_ring_of_fire_enable":
								if (text5 == "1")
								{
									config.Ele.RingOfFireEnable = true;
								}
								else if (text5 == "0")
								{
									config.Ele.RingOfFireEnable = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "ele_ring_of_fire_targets":
								if (int.TryParse(text5, out result))
								{
									config.Ele.RingOfFireTargets = result;
								}
								else
								{
									flag = true;
								}
								break;
							case "ele_thunderstorm_aoe":
								if (text5 == "1")
								{
									config.Ele.ThunderstormAoe = true;
								}
								else if (text5 == "0")
								{
									config.Ele.ThunderstormAoe = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "ele_thunderstorm_mana":
								if (int.TryParse(text5, out result))
								{
									config.Ele.ThunderstormMana = result;
								}
								else
								{
									flag = true;
								}
								break;
							case "ele_use_eng_gloves":
								if (text5 == "1")
								{
									config.Ele.UseEngGloves = true;
								}
								else if (text5 == "0")
								{
									config.Ele.UseEngGloves = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "ele_use_racial":
								if (text5 == "1")
								{
									config.Ele.UseRacial = true;
								}
								else if (text5 == "0")
								{
									config.Ele.UseRacial = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "ele_use_trinket1":
								if (text5 == "1")
								{
									config.Ele.UseTrinket1 = true;
								}
								else if (text5 == "0")
								{
									config.Ele.UseTrinket1 = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "ele_use_trinket2":
								if (text5 == "1")
								{
									config.Ele.UseTrinket2 = true;
								}
								else if (text5 == "0")
								{
									config.Ele.UseTrinket2 = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "ele_active_fire_totem":
								config.Ele.ActiveFireTotem = text5;
								break;
							case "ele_active_water_totem":
								config.Ele.ActiveWaterTotem = text5;
								break;
							case "ele_active_earth_totem":
								config.Ele.ActiveEarthTotem = text5;
								break;
							case "ele_active_air_totem":
								config.Ele.ActiveAirTotem = text5;
								break;
							case "ele_active_shield":
								config.Ele.ActiveShield = text5;
								break;
							case "ele_active_weapon":
								config.Ele.ActiveWeapon = text5;
								break;
							case "resto_use_riptide":
								if (text5 == "1")
								{
									config.Resto.UseRiptide = true;
								}
								else if (text5 == "0")
								{
									config.Resto.UseRiptide = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "resto_use_healing_wave":
								if (text5 == "1")
								{
									config.Resto.UseHealingWave = true;
								}
								else if (text5 == "0")
								{
									config.Resto.UseHealingWave = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "resto_use_chain_heal":
								if (text5 == "1")
								{
									config.Resto.UseChainHeal = true;
								}
								else if (text5 == "0")
								{
									config.Resto.UseChainHeal = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "resto_lesser_healing_wave_enable":
								if (text5 == "1")
								{
									config.Resto.LesserHealingWaveEnable = true;
								}
								else if (text5 == "0")
								{
									config.Resto.LesserHealingWaveEnable = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "resto_low_mana_lhw_enable":
								if (text5 == "1")
								{
									config.Resto.LowManaLhwEnable = true;
								}
								else if (text5 == "0")
								{
									config.Resto.LowManaLhwEnable = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "resto_use_mana_tide":
								if (text5 == "1")
								{
									config.Resto.UseManaTide = true;
								}
								else if (text5 == "0")
								{
									config.Resto.UseManaTide = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "resto_mana_tide_totem_percent":
								if (int.TryParse(text5, out result))
								{
									config.Resto.ManaTideTotemPercent = result;
								}
								else
								{
									flag = true;
								}
								break;
							case "resto_use_natures_swiftness":
								if (text5 == "1")
								{
									config.Resto.UseNaturesSwiftness = true;
								}
								else if (text5 == "0")
								{
									config.Resto.UseNaturesSwiftness = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "resto_heal_out_of_combat":
								if (text5 == "1")
								{
									config.Resto.HealOutOfCombat = true;
								}
								else if (text5 == "0")
								{
									config.Resto.HealOutOfCombat = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "resto_earth_shield_refresh":
								if (text5 == "1")
								{
									config.Resto.EarthShieldRefresh = true;
								}
								else if (text5 == "0")
								{
									config.Resto.EarthShieldRefresh = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "resto_earth_shield_focus":
								if (text5 == "1")
								{
									config.Resto.EarthShieldFocus = true;
								}
								else if (text5 == "0")
								{
									config.Resto.EarthShieldFocus = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "resto_earth_shield_name":
								config.Resto.EarthShieldName = text5;
								break;
							case "resto_valithria_enable":
								if (text5 == "1")
								{
									config.Resto.ValithriaEnable = true;
								}
								else if (text5 == "0")
								{
									config.Resto.ValithriaEnable = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "resto_riptide_tank":
								if (text5 == "1")
								{
									config.Resto.RiptideTank = true;
								}
								else if (text5 == "0")
								{
									config.Resto.RiptideTank = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "resto_overheal_percent":
								if (int.TryParse(text5, out result))
								{
									config.Resto.AllowedOverhealPct = result;
								}
								else
								{
									flag = true;
								}
								break;
							case "resto_use_eng_gloves":
								if (text5 == "1")
								{
									config.Resto.UseEngGloves = true;
								}
								else if (text5 == "0")
								{
									config.Resto.UseEngGloves = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "resto_use_racial":
								if (text5 == "1")
								{
									config.Resto.UseRacial = true;
								}
								else if (text5 == "0")
								{
									config.Resto.UseRacial = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "resto_use_trinket1":
								if (text5 == "1")
								{
									config.Resto.UseTrinket1 = true;
								}
								else if (text5 == "0")
								{
									config.Resto.UseTrinket1 = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "resto_use_trinket2":
								if (text5 == "1")
								{
									config.Resto.UseTrinket2 = true;
								}
								else if (text5 == "0")
								{
									config.Resto.UseTrinket2 = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "resto_active_fire_totem":
								config.Resto.ActiveFireTotem = text5;
								break;
							case "resto_active_water_totem":
								config.Resto.ActiveWaterTotem = text5;
								break;
							case "resto_active_earth_totem":
								config.Resto.ActiveEarthTotem = text5;
								break;
							case "resto_active_air_totem":
								config.Resto.ActiveAirTotem = text5;
								break;
							case "resto_active_shield":
								config.Resto.ActiveShield = text5;
								break;
							case "resto_active_weapon":
								config.Resto.ActiveWeapon = text5;
								break;
							case "common_selected_spec":
								config.Common.SelectedSpec = text5;
								break;
							case "common_use_wind_shear":
								if (text5 == "1")
								{
									config.Common.UseWindShear = true;
								}
								else if (text5 == "0")
								{
									config.Common.UseWindShear = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "common_use_purge":
								if (text5 == "1")
								{
									config.Common.UsePurge = true;
								}
								else if (text5 == "0")
								{
									config.Common.UsePurge = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "common_use_cleanse_spirit":
								if (text5 == "1")
								{
									config.Common.UseCleanseSpirit = true;
								}
								else if (text5 == "0")
								{
									config.Common.UseCleanseSpirit = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "common_use_cure_disease":
								if (text5 == "1")
								{
									config.Common.UseCureDisease = true;
								}
								else if (text5 == "0")
								{
									config.Common.UseCureDisease = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "common_totem_alive_time":
								if (int.TryParse(text5, out result))
								{
									config.Common.TotemAliveTime = result;
								}
								else
								{
									flag = true;
								}
								break;
							case "common_totem_recall":
								if (text5 == "1")
								{
									config.Common.TotemRecall = true;
								}
								else if (text5 == "0")
								{
									config.Common.TotemRecall = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "common_enable_regen":
								if (text5 == "1")
								{
									config.Common.EnableRegen = true;
								}
								else if (text5 == "0")
								{
									config.Common.EnableRegen = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "common_hp_regen_pct":
								if (int.TryParse(text5, out result))
								{
									config.Common.HpRegenPct = result;
								}
								else
								{
									flag = true;
								}
								break;
							case "common_mp_regen_pct":
								if (int.TryParse(text5, out result))
								{
									config.Common.MpRegenPct = result;
								}
								else
								{
									flag = true;
								}
								break;
							case "common_use_saronite_bomb":
								if (text5 == "1")
								{
									config.Common.UseSaroniteBomb = true;
								}
								else if (text5 == "0")
								{
									config.Common.UseSaroniteBomb = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "common_use_thermal_sapper":
								if (text5 == "1")
								{
									config.Common.UseThermalSapper = true;
								}
								else if (text5 == "0")
								{
									config.Common.UseThermalSapper = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "common_use_fel_healthstone":
								if (text5 == "1")
								{
									config.Common.UseFelHealthstone = true;
								}
								else if (text5 == "0")
								{
									config.Common.UseFelHealthstone = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "common_use_runic_healing_potion":
								if (text5 == "1")
								{
									config.Common.UseRunicHealingPotion = true;
								}
								else if (text5 == "0")
								{
									config.Common.UseRunicHealingPotion = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "common_use_runic_mana_potion":
								if (text5 == "1")
								{
									config.Common.UseRunicManaPotion = true;
								}
								else if (text5 == "0")
								{
									config.Common.UseRunicManaPotion = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "common_use_potion_speed_combat":
								if (text5 == "1")
								{
									config.Common.UsePotionSpeedCombat = true;
								}
								else if (text5 == "0")
								{
									config.Common.UsePotionSpeedCombat = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "common_use_potion_speed_prepot":
								if (text5 == "1")
								{
									config.Common.UsePotionSpeedPrepot = true;
								}
								else if (text5 == "0")
								{
									config.Common.UsePotionSpeedPrepot = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "common_use_potion_wild_magic_combat":
								if (text5 == "1")
								{
									config.Common.UsePotionWildMagicCombat = true;
								}
								else if (text5 == "0")
								{
									config.Common.UsePotionWildMagicCombat = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "common_use_potion_wild_magic_prepot":
								if (text5 == "1")
								{
									config.Common.UsePotionWildMagicPrepot = true;
								}
								else if (text5 == "0")
								{
									config.Common.UsePotionWildMagicPrepot = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "common_dbm_bars":
								config.Common.DbmBars = text5;
								break;
							case "common_use_flask_distilled_wisdom":
								if (text5 == "1")
								{
									config.Common.UseFlaskDistilledWisdom = true;
								}
								else if (text5 == "0")
								{
									config.Common.UseFlaskDistilledWisdom = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "common_use_flask_toughness":
								if (text5 == "1")
								{
									config.Common.UseFlaskToughness = true;
								}
								else if (text5 == "0")
								{
									config.Common.UseFlaskToughness = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "common_use_flask_resistance":
								if (text5 == "1")
								{
									config.Common.UseFlaskResistance = true;
								}
								else if (text5 == "0")
								{
									config.Common.UseFlaskResistance = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "common_use_elixir_agility":
								if (text5 == "1")
								{
									config.Common.UseElixirAgility = true;
								}
								else if (text5 == "0")
								{
									config.Common.UseElixirAgility = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "common_use_elixir_mighty_mageblood":
								if (text5 == "1")
								{
									config.Common.UseElixirMightyMageblood = true;
								}
								else if (text5 == "0")
								{
									config.Common.UseElixirMightyMageblood = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "common_use_elixir_mighty_spirit":
								if (text5 == "1")
								{
									config.Common.UseElixirMightySpirit = true;
								}
								else if (text5 == "0")
								{
									config.Common.UseElixirMightySpirit = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "common_use_elixir_mighty_thoughts":
								if (text5 == "1")
								{
									config.Common.UseElixirMightyThoughts = true;
								}
								else if (text5 == "0")
								{
									config.Common.UseElixirMightyThoughts = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "common_use_elixir_ogre_str":
								if (text5 == "1")
								{
									config.Common.UseElixirOgreStr = true;
								}
								else if (text5 == "0")
								{
									config.Common.UseElixirOgreStr = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "common_use_elixir_mighty_defense":
								if (text5 == "1")
								{
									config.Common.UseElixirMightyDefense = true;
								}
								else if (text5 == "0")
								{
									config.Common.UseElixirMightyDefense = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "common_use_flask_endless_rage":
								if (text5 == "1")
								{
									config.Common.UseFlaskEndlessRage = true;
								}
								else if (text5 == "0")
								{
									config.Common.UseFlaskEndlessRage = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "common_use_flask_frost_wyrm":
								if (text5 == "1")
								{
									config.Common.UseFlaskFrostWyrm = true;
								}
								else if (text5 == "0")
								{
									config.Common.UseFlaskFrostWyrm = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "common_use_flask_north":
								if (text5 == "1")
								{
									config.Common.UseFlaskNorth = true;
								}
								else if (text5 == "0")
								{
									config.Common.UseFlaskNorth = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "common_use_flask_pure_mojo":
								if (text5 == "1")
								{
									config.Common.UseFlaskPureMojo = true;
								}
								else if (text5 == "0")
								{
									config.Common.UseFlaskPureMojo = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "common_use_flask_stoneblood":
								if (text5 == "1")
								{
									config.Common.UseFlaskStoneblood = true;
								}
								else if (text5 == "0")
								{
									config.Common.UseFlaskStoneblood = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "common_use_elixir_armor_pierce":
								if (text5 == "1")
								{
									config.Common.UseElixirArmorPierce = true;
								}
								else if (text5 == "0")
								{
									config.Common.UseElixirArmorPierce = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "common_use_elixir_deadly_strikes":
								if (text5 == "1")
								{
									config.Common.UseElixirDeadlyStrikes = true;
								}
								else if (text5 == "0")
								{
									config.Common.UseElixirDeadlyStrikes = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "common_use_elixir_expertise":
								if (text5 == "1")
								{
									config.Common.UseElixirExpertise = true;
								}
								else if (text5 == "0")
								{
									config.Common.UseElixirExpertise = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "common_use_elixir_greater_str":
								if (text5 == "1")
								{
									config.Common.UseElixirGreaterStr = true;
								}
								else if (text5 == "0")
								{
									config.Common.UseElixirGreaterStr = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "common_use_elixir_mighty_str":
								if (text5 == "1")
								{
									config.Common.UseElixirMightyStr = true;
								}
								else if (text5 == "0")
								{
									config.Common.UseElixirMightyStr = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "common_use_elixir_lightning_speed":
								if (text5 == "1")
								{
									config.Common.UseElixirLightningSpeed = true;
								}
								else if (text5 == "0")
								{
									config.Common.UseElixirLightningSpeed = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "common_use_elixir_mastery":
								if (text5 == "1")
								{
									config.Common.UseElixirMastery = true;
								}
								else if (text5 == "0")
								{
									config.Common.UseElixirMastery = false;
								}
								else
								{
									flag = true;
								}
								break;
							case "common_use_elixir_spellpower":
								if (text5 == "1")
								{
									config.Common.UseElixirSpellpower = true;
								}
								else if (text5 == "0")
								{
									config.Common.UseElixirSpellpower = false;
								}
								else
								{
									flag = true;
								}
								break;
							default:
								flag = true;
								break;
							case "dummy":
								break;
							}
							continue;
						}
						flag = true;
						break;
					}
					HashSet<string> hashSet2 = new HashSet<string>();
					hashSet2.Add("ele_use_fire_elemental");
					hashSet2.Add("ele_use_flame_shock");
					hashSet2.Add("ele_use_lava_burst");
					hashSet2.Add("ele_use_lightning_bolt");
					hashSet2.Add("ele_use_chain_lightning");
					hashSet2.Add("ele_use_earth_shock");
					hashSet2.Add("ele_use_frost_shock");
					hashSet2.Add("ele_use_thunderstorm");
					hashSet2.Add("ele_use_elemental_mastery");
					hashSet2.Add("ele_chain_lightning_enable");
					hashSet2.Add("ele_chain_lightning_aoe");
					hashSet2.Add("ele_ring_of_fire_enable");
					hashSet2.Add("ele_ring_of_fire_targets");
					hashSet2.Add("ele_thunderstorm_aoe");
					hashSet2.Add("ele_thunderstorm_mana");
					hashSet2.Add("ele_use_eng_gloves");
					hashSet2.Add("ele_use_racial");
					hashSet2.Add("ele_use_trinket1");
					hashSet2.Add("ele_use_trinket2");
					hashSet2.Add("ele_active_fire_totem");
					hashSet2.Add("ele_active_water_totem");
					hashSet2.Add("ele_active_earth_totem");
					hashSet2.Add("ele_active_air_totem");
					hashSet2.Add("ele_active_shield");
					hashSet2.Add("ele_active_weapon");
					hashSet2.Add("resto_use_riptide");
					hashSet2.Add("resto_use_healing_wave");
					hashSet2.Add("resto_use_chain_heal");
					hashSet2.Add("resto_lesser_healing_wave_enable");
					hashSet2.Add("resto_low_mana_lhw_enable");
					hashSet2.Add("resto_use_mana_tide");
					hashSet2.Add("resto_mana_tide_totem_percent");
					hashSet2.Add("resto_use_natures_swiftness");
					hashSet2.Add("resto_heal_out_of_combat");
					hashSet2.Add("resto_earth_shield_refresh");
					hashSet2.Add("resto_earth_shield_focus");
					hashSet2.Add("resto_earth_shield_name");
					hashSet2.Add("resto_valithria_enable");
					hashSet2.Add("resto_riptide_tank");
					hashSet2.Add("resto_overheal_percent");
					hashSet2.Add("resto_use_eng_gloves");
					hashSet2.Add("resto_use_racial");
					hashSet2.Add("resto_use_trinket1");
					hashSet2.Add("resto_use_trinket2");
					hashSet2.Add("resto_active_fire_totem");
					hashSet2.Add("resto_active_water_totem");
					hashSet2.Add("resto_active_earth_totem");
					hashSet2.Add("resto_active_air_totem");
					hashSet2.Add("resto_active_shield");
					hashSet2.Add("resto_active_weapon");
					hashSet2.Add("common_selected_spec");
					hashSet2.Add("common_use_wind_shear");
					hashSet2.Add("common_use_purge");
					hashSet2.Add("common_use_cleanse_spirit");
					hashSet2.Add("common_use_cure_disease");
					hashSet2.Add("common_totem_alive_time");
					hashSet2.Add("common_totem_recall");
					hashSet2.Add("common_enable_regen");
					hashSet2.Add("common_hp_regen_pct");
					hashSet2.Add("common_mp_regen_pct");
					hashSet2.Add("common_use_saronite_bomb");
					hashSet2.Add("common_use_thermal_sapper");
					hashSet2.Add("common_use_fel_healthstone");
					hashSet2.Add("common_use_runic_healing_potion");
					hashSet2.Add("common_use_runic_mana_potion");
					hashSet2.Add("common_use_potion_speed_combat");
					hashSet2.Add("common_use_potion_speed_prepot");
					hashSet2.Add("common_use_potion_wild_magic_combat");
					hashSet2.Add("common_use_potion_wild_magic_prepot");
					hashSet2.Add("common_dbm_bars");
					hashSet2.Add("common_use_flask_distilled_wisdom");
					hashSet2.Add("common_use_flask_toughness");
					hashSet2.Add("common_use_flask_resistance");
					hashSet2.Add("common_use_elixir_agility");
					hashSet2.Add("common_use_elixir_mighty_mageblood");
					hashSet2.Add("common_use_elixir_mighty_spirit");
					hashSet2.Add("common_use_elixir_mighty_thoughts");
					hashSet2.Add("common_use_elixir_ogre_str");
					hashSet2.Add("common_use_elixir_mighty_defense");
					hashSet2.Add("common_use_flask_endless_rage");
					hashSet2.Add("common_use_flask_frost_wyrm");
					hashSet2.Add("common_use_flask_north");
					hashSet2.Add("common_use_flask_pure_mojo");
					hashSet2.Add("common_use_flask_stoneblood");
					hashSet2.Add("common_use_elixir_armor_pierce");
					hashSet2.Add("common_use_elixir_deadly_strikes");
					hashSet2.Add("common_use_elixir_expertise");
					hashSet2.Add("common_use_elixir_greater_str");
					hashSet2.Add("common_use_elixir_mighty_str");
					hashSet2.Add("common_use_elixir_lightning_speed");
					hashSet2.Add("common_use_elixir_mastery");
					hashSet2.Add("common_use_elixir_spellpower");
					HashSet<string> hashSet3 = hashSet2;
					if (!hashSet.SetEquals(hashSet3))
					{
						List<string> list = hashSet3.Except(hashSet).ToList();
						List<string> list2 = hashSet.Except(hashSet3).ToList();
						Logging.Write("[CONFIG] Payload received, but keys mismatch!");
						Logging.Write("[CONFIG] Expected keys count: " + hashSet3.Count);
						Logging.Write("[CONFIG] Received keys count: " + hashSet.Count);
						if (list.Count > 0)
						{
							Logging.Write("[CONFIG] MISSING keys (" + list.Count + "):");
							foreach (string item in list)
							{
								Logging.Write("  - " + item);
							}
						}
						if (list2.Count > 0)
						{
							Logging.Write("[CONFIG] EXTRA keys (" + list2.Count + "):");
							foreach (string item2 in list2)
							{
								Logging.Write("  + " + item2);
							}
						}
						flag = true;
					}
					else
					{
						uint tickCount = (uint)Environment.TickCount;
						if (tickCount - _lastConfigReadyLog > 5000 || _lastConfigReadyLog == 0)
						{
							Logging.Write("[CONFIG] Keys match! Config READY.");
							_lastConfigReadyLog = tickCount;
						}
					}
					if (!flag)
					{
						lock (_cacheLock)
						{
							_config = config;
						}
					}
				}
			}
			catch (Exception ex)
			{
				if ((uint)(Environment.TickCount - (int)_lastCatchLog) > 5000u)
				{
					Logging.WriteError("ConfigLoop error: " + ex.Message, true);
					_lastCatchLog = (uint)Environment.TickCount;
				}
			}
			Thread.Sleep(100);
		}
	}

	private void RotationLoop()
	{
		while (_isLaunched)
		{
			try
			{
				if (Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause)
				{
					FSM_Tick();
				}
			}
			catch (Exception ex)
			{
				FT("!!! FSM EXCEPTION !!!");
				FTLine(ex.ToString());
				Logging.WriteError("[Shaman FSM Error] " + ex.ToString(), true);
			}
			Thread.Sleep(50);
		}
	}

    private bool IsEffectiveCombat()
    {
        // 05. COMBAT DETECTION: Canonical Combat Source
        // The ONLY reliable source of combat truth is the game engine.
        // False positives from Target aggro, Auto-attack, or Debuffs cause stale states and logic oscillation.
        return ObjectManager.Me.InCombatFlagOnly;
    }
    
    private void FSM_Tick()
    {
        _fsmTickId++;
        _policyValid = false;
        long traceId = _traceTick + 1;
        FT("============================================================");
        FT("FSM_TICK START");

        ConfigCache c;
        lock (_cacheLock) { c = _config; }

        FTLine("SPEC = " + c.Common.SelectedSpec);
        bool inCombat = IsEffectiveCombat();
        bool isResto = c.Common.SelectedSpec.Contains("Resto");
        bool isEle = c.Common.SelectedSpec.Contains("Ele");

        FT("[COMBAT TRACE]");
        FTLine("FSMTick=" + _fsmTickId);
        FTLine("COMBAT=" + inCombat);
        FTLine("ENGINE=" + ObjectManager.Me.InCombatFlagOnly);
        WoWUnit currentTarget = ObjectManager.Target;
        FTLine("TARGET_VALID=" + (currentTarget != null && currentTarget.IsValid));
        FTLine("TARGET_ALIVE=" + (currentTarget != null && currentTarget.IsAlive));
        FTLine("TARGET_ATTACKABLE=" + (currentTarget != null && currentTarget.IsAttackable));
        FTLine("PLAYER_DEAD=" + !ObjectManager.Me.IsAlive);
        FTLine("REASON=EngineFlagOnly");
        
        if (!inCombat)
        {
            FT("OOC PRIORITY CHAIN START");
            
            FTStateStart("OOC.State_TotemCombatOverrides");
            bool r1 = State_TotemCombatOverrides(c, isResto);
            FTResult("OOC.State_TotemCombatOverrides", r1);
            FTStateEnd("OOC.State_TotemCombatOverrides");
            if (r1) { FT("FSM_TICK END"); return; }
            
            FTStateStart("OOC.State_TotemBasePreset");
            bool r2 = State_TotemBasePreset(c, isResto);
            FTResult("OOC.State_TotemBasePreset", r2);
            FTStateEnd("OOC.State_TotemBasePreset");
            if (r2) { FT("FSM_TICK END"); return; }

            FTStateStart("OOC.State_BuffsAndTotems");
            bool r3 = State_BuffsAndTotems(c, isResto);
            FTResult("OOC.State_BuffsAndTotems", r3);
            FTStateEnd("OOC.State_BuffsAndTotems");
            if (r3) { FT("FSM_TICK END"); return; }
            
            if (isResto)
            {
                FTStateStart("OOC.State_CoreRotation_Resto");
                State_CoreRotation_Resto(c);
                FTStateEnd("OOC.State_CoreRotation_Resto");
            }
            
            FT("OOC RETURN");
            FT("FSM_TICK END");
            return;
        }

        FT("COMBAT PRIORITY CHAIN START");
        
        FTStateStart("State_Universal_Reactions");
        bool uni = State_Universal_Reactions(c);
        FTResult("State_Universal_Reactions", uni);
        FTStateEnd("State_Universal_Reactions");
        if (uni) { FT("FSM_TICK END"); return; }

        FTStateStart("State_DBM_Precast");
        bool dbm = State_DBM_Precast(c, isResto);
        FTResult("State_DBM_Precast", dbm);
        FTStateEnd("State_DBM_Precast");
        if (dbm) { FT("FSM_TICK END"); return; }
        
        FTStateStart("State_TotemCombatOverrides");
        bool tover = State_TotemCombatOverrides(c, isResto);
        FTResult("State_TotemCombatOverrides", tover);
        FTStateEnd("State_TotemCombatOverrides");
        if (tover) { FT("FSM_TICK END"); return; }

        FTStateStart("State_TotemBasePreset");
        bool totemBase = State_TotemBasePreset(c, isResto);
        FTResult("State_TotemBasePreset", totemBase);
        FTStateEnd("State_TotemBasePreset");
        if (totemBase) { FT("FSM_TICK END"); return; }

        FT("[DPS GATE]");
        
        bool dpsGateOpen = IsBaseTotemReadyForDps();
        string gateReason = "OPEN (DPS Allowed)";
        if (!dpsGateOpen)
        {
            gateReason = "CLOSED (DPS Policy Blocked or Waiting for Call)";
            if (_totemState == TotemPresetState.Called && _lastRestoreAction == TotemRestoreAction.GLOBAL_CALL) gateReason = "CLOSED (Waiting for Global Call API)";
        }
        
        FTLine("FSMTick=" + _fsmTickId);
        FTLine("PolicyTick=" + _policyTickId);
        FTLine("PolicyValid=" + _policyValid.ToString());
        FTLine("FSM_STATE=" + _totemState.ToString());
        FTLine("RESTORE_ACTION=" + _lastRestoreAction.ToString());
        FTLine("BASE_VERIFIED=" + _lastBaseVerified.ToString());
        FTLine("OVERRIDE_ACTIVE=" + _lastOverrideActive.ToString());
        FTLine("SPECIAL_DPS=" + _lastSpecialDpsActive.ToString());
        FTLine("POLICY_RESULT=" + (_lastDpsPolicyAllow ? "ALLOW" : "BLOCK"));
        FTLine("POLICY_REASON=" + _lastDpsReason);
        FTLine("GATE_RESULT=" + (dpsGateOpen ? "OPEN" : "CLOSED"));
        FTLine("GATE_REASON=" + gateReason);
        
        if ((_lastDpsPolicyAllow && !dpsGateOpen && _totemState != TotemPresetState.Called && _lastRestoreAction != TotemRestoreAction.GLOBAL_CALL) || (!_lastDpsPolicyAllow && dpsGateOpen))
        {
            FTLine("[DPS GATE INVARIANT VIOLATION] Mismatch! PolicyValid=" + _policyValid + " FSMTick=" + _fsmTickId + " PolicyTick=" + _policyTickId + " Policy=" + (_lastDpsPolicyAllow ? "ALLOW" : "BLOCK") + " Gate=" + (dpsGateOpen ? "OPEN" : "CLOSED") + " State=" + _totemState.ToString() + " Restore=" + _lastRestoreAction.ToString());
        }

        if (dpsGateOpen)
        {
            FTStateStart("State_BuffsAndTotems");
            bool buffs = State_BuffsAndTotems(c, isResto);
            FTResult("State_BuffsAndTotems", buffs);
            FTStateEnd("State_BuffsAndTotems");
            if (buffs) { FT("FSM_TICK END"); return; }

            if (isResto)
            {
                FTStateStart("State_CoreRotation_Resto");
                State_CoreRotation_Resto(c);
                FTStateEnd("State_CoreRotation_Resto");
            }
            else if (isEle)
            {
                FTStateStart("State_CoreRotation_Ele");
                State_CoreRotation_Ele(c);
                FTStateEnd("State_CoreRotation_Ele");
            }
        }

        FT("FSM_TICK END");
    }

	private int GetTankScore(WoWUnit p, ConfigCache c)
	{
		int num = 0;
		if (((WoWObject)p).Name == c.Resto.EarthShieldName)
		{
			num += 500;
		}
		if ((float)p.MaxHealth > (float)((WoWUnit)ObjectManager.Me).MaxHealth * 1.5f)
		{
			num += 100;
		}
		bool flag = BuffManager.HaveBuff(((WoWObject)p).GetBaseAddress, 71u);
		bool flag2 = BuffManager.HaveBuff(((WoWObject)p).GetBaseAddress, 25780u);
		bool flag3 = BuffManager.HaveBuff(((WoWObject)p).GetBaseAddress, 48263u);
		bool flag4 = BuffManager.HaveBuff(((WoWObject)p).GetBaseAddress, 9634u);
		if (flag || flag2 || flag3 || flag4)
		{
			num += 300;
		}
		int num2 = ObjectManager.GetWoWUnitHostile().Count((WoWUnit u) => u.IsAlive && u.Target == ((WoWObject)p).Guid);
		num += num2 * 100;
		if (ObjectManager.GetWoWUnitHostile().Any((WoWUnit u) => u.IsAlive && u.IsElite && u.Target == ((WoWObject)p).Guid && u.MaxHealth > ((WoWUnit)ObjectManager.Me).MaxHealth * 3))
		{
			num += 400;
		}
		float recentDamageRate = GetRecentDamageRate(((WoWObject)p).Guid);
		if (recentDamageRate > 0f)
		{
			num += (int)(recentDamageRate / 5f);
		}
		return num;
	}

	private float GetTankBonus(WoWUnit p, WoWUnit tank, float bonusMultiplier = 1f)
	{
		if (tank != null && ((WoWObject)p).Guid == ((WoWObject)tank).Guid)
		{
			float num = 1000f;
			float num2 = 100f - (float)p.HealthPercent;
			float num3 = num2 * 30f;
			float num4 = ((p.HealthPercent < 30.0) ? 2000f : 0f);
			return (num + num3 + num4) * bonusMultiplier;
		}
		return 0f;
	}

		private void SafeSetTarget(WoWUnit target)
	{
		if (target != null && ((WoWObject)target).IsValid)
		{
			((WoWUnit)ObjectManager.Me).Target = ((WoWObject)target).Guid;
			wManager.Wow.Helpers.Interact.InteractGameObject(((WoWObject)target).GetBaseAddress, false, true);
		}
	}

	private WoWUnit GetCombatTarget(float fallbackRange = 40f)
	{
		WoWUnit target = ObjectManager.Target;
		if (target != null && ((WoWObject)target).IsValid && target.IsAlive && target.IsAttackable)
		{
			return target;
		}
		return (from u in ObjectManager.GetObjectWoWUnit()
			where ((WoWObject)u).IsValid && u.IsAlive && u.IsAttackable && ((WoWObject)u).GetDistance <= fallbackRange
			orderby ((WoWObject)u).GetDistance
			select u).FirstOrDefault();
	}

	private float CalcHealScore(WoWUnit p, WoWUnit tank, float spellHeal, ConfigCache c, float allowedOverhealPct, float castTime, float tankBias = 0f)
	{
		float num = (float)((100.0 - p.HealthPercent) / 100.0 * (double)p.MaxHealth);
		float recentDamageRate = GetRecentDamageRate(((WoWObject)p).Guid);
		float num2 = recentDamageRate * castTime;
		if (tank != null && ((WoWObject)p).Guid == ((WoWObject)tank).Guid && num2 < (float)p.MaxHealth * 0.05f * castTime)
		{
			num2 = (float)p.MaxHealth * 0.05f * castTime;
		}
		float num3 = num + num2;
		float num4 = (float)System.Math.Min((double)spellHeal, (double)num3);
		float num5 = spellHeal - num4;
		float num6 = spellHeal * (allowedOverhealPct / 100f);
		float num7 = (float)System.Math.Max(0.0, num5 - num6);
		return num4 - num7 * 2f + tankBias;
	}

	private uint ResolveSpell(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return 0u;
		}
		uint[] value; if (!SpellRanks.TryGetValue(name, out value))
		{
			return 0u;
		}
		uint[] array = value;
		uint[] array2 = array;
		foreach (uint num in array2)
		{
			if (SpellManager.KnowSpell(num))
			{
				return num;
			}
		}
		return 0u;
	}

	private float GetExpectedHeal(uint spellId, float bonusHealing)
	{
		switch (spellId)
		{
		case 49273u:
			return 3000f + bonusHealing * 0.857f;
		case 25357u:
			return 2100f + bonusHealing * 0.857f;
		case 10627u:
			return 1600f + bonusHealing * 0.857f;
		case 10395u:
			return 1300f + bonusHealing * 0.857f;
		case 939u:
			return 1000f + bonusHealing * 0.857f;
		case 547u:
			return 700f + bonusHealing * 0.857f;
		case 331u:
			return 400f + bonusHealing * 0.857f;
		case 49276u:
			return 1600f + bonusHealing * 0.428f;
		case 25420u:
			return 1000f + bonusHealing * 0.428f;
		case 10468u:
			return 800f + bonusHealing * 0.428f;
		case 8005u:
			return 600f + bonusHealing * 0.428f;
		case 8004u:
			return 400f + bonusHealing * 0.428f;
		case 8008u:
			return 200f + bonusHealing * 0.428f;
		case 55459u:
			return 1050f + bonusHealing * 0.714f;
		case 25423u:
			return 800f + bonusHealing * 0.714f;
		case 10623u:
			return 600f + bonusHealing * 0.714f;
		case 1064u:
			return 400f + bonusHealing * 0.714f;
		case 61295u:
		case 61301u:
			return 1670f + bonusHealing * 0.428f;
		case 61299u:
		case 61300u:
			return 1300f + bonusHealing * 0.428f;
		case 55340u:
			return 900f + bonusHealing * 0.428f;
		case 55339u:
			return 500f + bonusHealing * 0.428f;
		default:
			return 1000f + bonusHealing * 0.5f;
		}
	}

	private bool IsTotemSpellActive(int slot, uint spellId)
	{
		return Lua.LuaDoString<bool>("local have, name = GetTotemInfo(" + slot + "); return have and name == GetSpellInfo(" + spellId + ");", "");
	}

	private bool HasOffensiveGloveTinker()
	{
		return Lua.LuaDoString<bool>("local id = GetInventoryItemID('player',10); if not id then return false end;local link = GetInventoryItemLink('player',10); if not link then return false end;return link:find('54998') ~= nil or link:find('54999') ~= nil or link:find('55002') ~= nil;", "");
	}

	private void LogAction(string action)
	{
		Logging.Write("[Shaman PVE] " + action);
	}

	private void AddExpectedState(string state, int durationMs = 500)
	{
		lock (_stateLock)
		{
			_expectedStates[state] = (uint)(Environment.TickCount + durationMs);
		}
	}

	private bool HasExpectedState(string state)
	{
		lock (_stateLock)
		{
			if (!_expectedStates.ContainsKey(state)) return false;
			// Auto-expire: compare as signed to handle uint wraparound
			if ((int)(_expectedStates[state] - (uint)Environment.TickCount) <= 0)
			{
				_expectedStates.Remove(state);
				return false;
			}
			return true;
		}
	}

	private bool IsBaseTotemReadyForDps()
	{
        if (_totemState == TotemPresetState.Verified) return true;
        if (!_policyValid || _policyTickId != _fsmTickId) {
            FTLine("[DPS GATE ERROR] STALE POLICY CACHE DETECTED! FSMTick=" + _fsmTickId + " PolicyTick=" + _policyTickId + " PolicyValid=" + _policyValid);
            return false;
        }
        if (_totemState == TotemPresetState.ReadyToCall) return _lastDpsPolicyAllow;
        if (_totemState == TotemPresetState.Called) {
            if (_lastRestoreAction == TotemRestoreAction.PARTIAL_FALLBACK) return _lastDpsPolicyAllow;
            return false;
        }
        return false;
	}



    private bool IsTargetPurgeable(out string auraName, out string auraType)
    {
        auraName = "";
        auraType = "";
        
        string lua = @"
            local bestName = ''
            local bestType = ''
            
            local whitelist = {
                ['Power Word: Shield'] = true,
                ['Ice Barrier'] = true,
                ['Bloodlust'] = true,
                ['Heroism'] = true,
                ['Rejuvenation'] = true,
                ['Regrowth'] = true,
                ['Divine Favor'] = true,
                ['Icy Veins'] = true
            }

            for i=1,40 do
                local name, _, _, _, buffType = UnitBuff('target', i)
                if not name then break end
                
                if buffType == 'Magic' then
                    if whitelist[name] then
                        return name .. '^' .. buffType .. '^1'
                    end
                    if bestName == '' then
                        bestName = name
                        bestType = buffType
                    end
                end
            end
            
            if bestName ~= '' then
                return bestName .. '^' .. bestType .. '^0'
            end
            
            return 'NONE'
        ";
        
        string result = Lua.LuaDoString<string>(lua, "");
        if (result == "NONE" || string.IsNullOrEmpty(result)) return false;
        
        string[] parts = result.Split('^');
        if (parts.Length >= 2)
        {
            auraName = parts[0];
            auraType = parts[1];
            return true;
        }
        return false;
    }

    private bool IsTargetInterruptible(out string spellName, out int timeLeftMs, out bool isChannel)
    {
        spellName = "";
        timeLeftMs = 0;
        isChannel = false;
        
        string lua = @"
            local spell, _, _, _, startTime, endTime, _, castID, notInterruptible = UnitCastingInfo('target');
            if spell then
                if notInterruptible then return 'NO_INTERRUPT' end
                local timeL = endTime - (GetTime() * 1000)
                return 'CAST^' .. spell .. '^' .. math.floor(timeL)
            end
            local spellC, _, _, _, startTimeC, endTimeC, _, notInterruptibleC = UnitChannelInfo('target');
            if spellC then
                if notInterruptibleC then return 'NO_INTERRUPT' end
                local timeC = endTimeC - (GetTime() * 1000)
                return 'CHANNEL^' .. spellC .. '^' .. math.floor(timeC)
            end
            return 'NONE'
        ";
        
        string result = Lua.LuaDoString<string>(lua, "");
        if (result == "NONE" || string.IsNullOrEmpty(result)) return false;
        if (result == "NO_INTERRUPT") return false;
        
        string[] parts = result.Split('^');
        if (parts.Length == 3)
        {
            isChannel = parts[0] == "CHANNEL";
            spellName = parts[1];
            int.TryParse(parts[2], out timeLeftMs);
            return true;
        }
        return false;
    }


    private bool State_Universal_Reactions(ConfigCache c)
    {
        WoWUnit combatTarget = GetCombatTarget();

        // 1. Wind Shear
        if (c.Common.UseWindShear && combatTarget != null && ((WoWObject)combatTarget).IsValid && combatTarget.IsAttackable && combatTarget.GetDistance <= 25f && combatTarget.IsCast)
        {
            uint num = ResolveSpell("wind_shear");
            if (num != 0 && SpellManager.GetSpellCooldownTimeLeft(num) <= 0 && !HasExpectedState("WindShear"))
            {
                // Target Sync must complete BEFORE checking Lua, because Lua targets 'target'
                if (((WoWUnit)ObjectManager.Me).Target != ((WoWObject)combatTarget).Guid)
                {
                    FTLine("[TARGET SWITCH] FSMTick=" + _fsmTickId + " FROM=" + (((WoWUnit)ObjectManager.Me).Target) + " TO=" + combatTarget.Name + " REASON=Wind Shear Target Sync");
                    SafeSetTarget(combatTarget);
                    return false; // YIELD
                }

                string spellName;
                int timeLeftMs;
                bool isChannel;
                
                bool isInterruptible = IsTargetInterruptible(out spellName, out timeLeftMs, out isChannel);
                
                FTLine("[WIND SHEAR] CANDIDATE=" + combatTarget.Name + " CASTING=" + (!isChannel && isInterruptible) + " CHANNELING=" + (isChannel && isInterruptible) + " SPELL=" + spellName + " REMAINING=" + timeLeftMs + "ms INTERRUPTIBLE=" + isInterruptible + " DISTANCE=" + combatTarget.GetDistance.ToString("0.0") + " LOS=True");

                if (isInterruptible)
                {
                    // Late interrupt prevention for CASTS (prevent wasting CD when <300ms remains)
                    if (!isChannel && timeLeftMs < 300 && timeLeftMs > 0)
                    {
                        FTLine("[WIND SHEAR BLOCK] FSMTick=" + _fsmTickId + " REASON=Late Cast (" + timeLeftMs + "ms remaining)");
                    }
                    else
                    {
                        FTLine("[WIND SHEAR CAST] FSMTick=" + _fsmTickId + " TARGET=" + combatTarget.Name + " SPELL=" + spellName + " REASON=Interrupt");
                        SpellManager.CastSpellByIdLUA(num);
                        Logging.Write("[REACT] Wind Shear (" + num + ") on " + spellName);
                        AddExpectedState("WindShear", 1500);
                        return true;
                    }
                }
                else
                {
                    FTLine("[WIND SHEAR BLOCK] FSMTick=" + _fsmTickId + " REASON=Not Interruptible or Finished");
                }
            }
            else if (num == 0)
            {
                // Silent block for unknown spell
            }
        }
        
		bool flag = c.Common.UseCleanseSpirit && ResolveSpell("cleanse_spirit") != 0 && SpellManager.GetSpellCooldownTimeLeft(ResolveSpell("cleanse_spirit")) <= 0 && !HasExpectedState("Cleanse");
		bool flag2 = c.Common.UseCureDisease && ResolveSpell("cure_disease") != 0 && SpellManager.GetSpellCooldownTimeLeft(ResolveSpell("cure_disease")) <= 0 && !HasExpectedState("Cure");
		if (flag || flag2)
		{
			string text = Lua.LuaDoString<string>("local canCleanse = " + (flag ? "true" : "false") + "; local canCure = " + (flag2 ? "true" : "false") + ";\nlocal targets = {'player'}\nfor i=1,4 do table.insert(targets, 'party'..i) end\nfor i=1,40 do table.insert(targets, 'raid'..i) end\n\nlocal function isValid(t)\n    return UnitExists(t) and UnitIsVisible(t) and not UnitIsDead(t) and (t == 'player' or IsSpellInRange(GetSpellInfo(51886), t) == 1 or IsSpellInRange(GetSpellInfo(526), t) == 1)\nend\n\nlocal bestCurse, bestDisease, bestPoison\nfor _, t in ipairs(targets) do\n    if isValid(t) then\n        for i=1,40 do\n            local name,_,_,_,type = UnitDebuff(t, i)\n            if not name then break end\n            if canCleanse and type == 'Curse' and not bestCurse then bestCurse = t..'|Curse' end\n            if (canCleanse or canCure) and type == 'Disease' and not bestDisease then bestDisease = t..'|Disease' end\n            if (canCleanse or canCure) and type == 'Poison' and not bestPoison then bestPoison = t..'|Poison' end\n        end\n    end\nend\nif bestCurse then return bestCurse end\nif bestDisease then return bestDisease end\nif bestPoison then return bestPoison end\n\nreturn ''\n", "");
			if (text != "")
			{
				string[] array = text.Split('|');
				if (array.Length == 2)
				{
					string text2 = array[0];
					string text3 = array[1];
					if (text3 == "Curse" && flag)
					{
						if (text2 == "player" || Lua.LuaDoString<bool>("return IsSpellInRange(GetSpellInfo(51886), '" + text2 + "') == 1", ""))
						{
							if (!Lua.LuaDoString<bool>("return UnitIsUnit('target', '" + text2 + "')", ""))
							{
								Lua.LuaDoString("TargetUnit('" + text2 + "')", false);
								return true;
							}
							SpellManager.CastSpellByIdLUA(ResolveSpell("cleanse_spirit"));
							AddExpectedState("Cleanse", 1500);
							return true;
						}
					}
					else if ((text3 == "Disease" || text3 == "Poison") && flag)
					{
						if (text2 == "player" || Lua.LuaDoString<bool>("return IsSpellInRange(GetSpellInfo(51886), '" + text2 + "') == 1", ""))
						{
							if (!Lua.LuaDoString<bool>("return UnitIsUnit('target', '" + text2 + "')", ""))
							{
								Lua.LuaDoString("TargetUnit('" + text2 + "')", false);
								return true;
							}
							SpellManager.CastSpellByIdLUA(ResolveSpell("cleanse_spirit"));
							AddExpectedState("Cleanse", 1500);
							return true;
						}
					}
					else if ((text3 == "Disease" || text3 == "Poison") && flag2 && (text2 == "player" || Lua.LuaDoString<bool>("return IsSpellInRange(GetSpellInfo(526), '" + text2 + "') == 1", "")))
					{
						if (!Lua.LuaDoString<bool>("return UnitIsUnit('target', '" + text2 + "')", ""))
						{
							Lua.LuaDoString("TargetUnit('" + text2 + "')", false);
							return true;
						}
						SpellManager.CastSpellByIdLUA(ResolveSpell("cure_disease"));
						AddExpectedState("Cure", 1500);
						return true;
					}
				}
			}
		}
		if (c.Common.EnableRegen && IsEffectiveCombat())
		{
			if (c.Common.UseFelHealthstone && ((WoWUnit)ObjectManager.Me).HealthPercent <= (double)c.Common.HpRegenPct && !HasExpectedState("Healthstone") && ItemsManager.HasItemById(36892u) && Lua.LuaDoString<bool>("local s,d = GetItemCooldown(36892); return d == 0;", ""))
			{
				ItemsManager.UseItem(36892u);
				AddExpectedState("Healthstone", 2000);
				return true;
			}
			if (c.Common.UseRunicHealingPotion && ((WoWUnit)ObjectManager.Me).HealthPercent <= (double)c.Common.HpRegenPct && !HasExpectedState("HealthPotion") && ItemsManager.HasItemById(33447u) && Lua.LuaDoString<bool>("local s,d = GetItemCooldown(33447); return d == 0;", ""))
			{
				ItemsManager.UseItem(33447u);
				AddExpectedState("HealthPotion", 2000);
				return true;
			}
			if (c.Common.UseRunicManaPotion && ((WoWUnit)ObjectManager.Me).ManaPercentage <= c.Common.MpRegenPct && !HasExpectedState("ManaPotion") && ItemsManager.HasItemById(33448u) && Lua.LuaDoString<bool>("local s,d = GetItemCooldown(33448); return d == 0;", ""))
			{
				ItemsManager.UseItem(33448u);
				AddExpectedState("ManaPotion", 2000);
				return true;
			}
		}

        // 2. Purge
        if (c.Common.UsePurge && combatTarget != null && ((WoWObject)combatTarget).IsValid && combatTarget.IsAlive && combatTarget.IsAttackable && combatTarget.GetDistance <= 30f && ((WoWUnit)ObjectManager.Me).ManaPercentage > 20)
        {
            uint num = ResolveSpell("purge");
            if (num != 0 && SpellManager.GetSpellCooldownTimeLeft(num) <= 0 && !HasExpectedState("Purge"))
            {
                // Target Sync must complete BEFORE checking Lua
                if (((WoWUnit)ObjectManager.Me).Target != ((WoWObject)combatTarget).Guid)
                {
                    FTLine("[TARGET SWITCH] FSMTick=" + _fsmTickId + " FROM=" + (((WoWUnit)ObjectManager.Me).Target) + " TO=" + combatTarget.Name + " REASON=Purge Target Sync");
                    SafeSetTarget(combatTarget);
                    return false; // YIELD
                }

                string auraName;
                string auraType;
                
                bool isPurgeable = IsTargetPurgeable(out auraName, out auraType);
                
                if (isPurgeable)
                {
                    FTLine("[PURGE] CANDIDATE=" + combatTarget.Name + " AURA=" + auraName + " AURA_TYPE=" + auraType + " DISPELLABLE=True DISTANCE=" + combatTarget.GetDistance.ToString("0.0") + " LOS=True");
                    FTLine("[PURGE CAST] FSMTick=" + _fsmTickId + " TARGET=" + combatTarget.Name + " AURA=" + auraName + " AURA_TYPE=" + auraType + " REASON=Beneficial Magic");
                    SpellManager.CastSpellByIdLUA(num);
                    Logging.Write("[REACT] Purge (" + num + ") on " + auraName);
                    AddExpectedState("Purge", 1500); // Anti-spam
                    return true;
                }
            }
        }
        
// Clean Cache Buster
