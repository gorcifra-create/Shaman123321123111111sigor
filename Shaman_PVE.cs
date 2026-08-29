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

	private bool State_Universal_Reactions(ConfigCache c)
	{
		WoWUnit combatTarget = GetCombatTarget();
		if (c.Common.UseWindShear && combatTarget != null && ((WoWObject)combatTarget).IsValid && combatTarget.IsAttackable && combatTarget.IsCast)
		{
			uint num = ResolveSpell("wind_shear");
			if (num != 0 && SpellManager.GetSpellCooldownTimeLeft(num) <= 0)
			{
				if (((WoWUnit)ObjectManager.Me).Target != ((WoWObject)combatTarget).Guid)
				{
					FTLine("[TARGET SWITCH] FSMTick=" + _fsmTickId + " FROM=" + (((WoWUnit)ObjectManager.Me).Target) + " TO=" + combatTarget.Name + " REASON=Wind Shear Target Sync");
					SafeSetTarget(combatTarget);
					return false;
				}
				SpellManager.CastSpellByIdLUA(num);
				Logging.Write("[REACT] Wind Shear (" + num + ")");
				return true;
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
		if (c.Common.UsePurge && combatTarget != null && combatTarget.IsAlive && combatTarget.IsAttackable && ((WoWUnit)ObjectManager.Me).ManaPercentage > 30)
		{
			if (((WoWUnit)ObjectManager.Me).Target != ((WoWObject)combatTarget).Guid)
				{
					FTLine("[TARGET SWITCH] FSMTick=" + _fsmTickId + " FROM=" + (((WoWUnit)ObjectManager.Me).Target) + " TO=" + combatTarget.Name + " REASON=Wind Shear Target Sync");
					SafeSetTarget(combatTarget);
					return false;
				}
			if (Lua.LuaDoString<bool>("for i=1,40 do local name,_,_,_,type = UnitBuff('target', i); if name and (name == 'Power Word: Shield' or name == 'Ice Barrier' or name == 'Bloodlust' or name == 'Heroism' or name == 'Rejuvenation' or name == 'Regrowth' or (type == 'Magic' and UnitMana('player')/UnitManaMax('player') > 0.5)) then return true end end return false;", "") && ResolveSpell("purge") != 0 && SpellManager.GetSpellCooldownTimeLeft(ResolveSpell("purge")) <= 0 && !HasExpectedState("Purge" + ((WoWObject)combatTarget).Guid))
			{
				SpellManager.CastSpellByIdLUA(ResolveSpell("purge"));
				AddExpectedState("Purge" + ((WoWObject)combatTarget).Guid, 1500);
				return true;
			}
		}
		return false;
	}

	private bool IsBossLikeTarget(WoWUnit u)
	{
		if (u == null || !((WoWObject)u).IsValid)
		{
			return false;
		}
		return u.IsBoss || (u.Level == 83 && u.MaxHealth > 1000000);
	}

	private bool State_DBM_Precast(ConfigCache c, bool isResto)
	{
		//IL_025c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0263: Expected O, but got Unknown
		if (string.IsNullOrEmpty(c.Common.DbmBars))
		{
			return false;
		}
		string[] array = c.Common.DbmBars.Split(new char[1] { '^' }, StringSplitOptions.RemoveEmptyEntries);
		string[] array2 = array;
		string[] array3 = array2;
		foreach (string text in array3)
		{
			string[] array4 = text.Split(':');
			if (array4.Length != 2)
			{
				continue;
			}
			string text2 = array4[0].ToLower();
			float result; if (!float.TryParse(array4[1], NumberStyles.Any, CultureInfo.InvariantCulture, out result))
			{
				continue;
			}
			if ((text2.Contains("meteor") || text2.Contains("defile") || text2.Contains("blistering")) && result <= 2f)
			{
				if (ObjectManager.Me.IsCast)
				{
					Lua.LuaDoString("SpellStopCasting();", false);
					Logging.Write("[God-Tier] DANGER DETECTED: StopCasting!");
				}
				return true;
			}
			if (!isResto || (!text2.Contains("bonestorm") && !text2.Contains("infest")) || !(result <= 2.5f) || ObjectManager.Me.IsCast || !c.Resto.UseChainHeal || ResolveSpell("chain_heal") == 0)
			{
				continue;
			}
			List<WoWUnit> precastTargets = ObjectManager.GetObjectWoWPlayer().Where(delegate(WoWPlayer u)
			{
				//IL_000a: Unknown result type (might be due to invalid IL or missing references)
				//IL_0010: Invalid comparison between Unknown and I4
				return ((WoWUnit)u).IsAlive && (int)((WoWUnit)u).Reaction >= 4 && ((WoWObject)u).GetDistance <= 40f && !TraceLine.TraceLineGo(((WoWObject)ObjectManager.Me).Position, ((WoWObject)u).Position, (CGWorldFrameHitFlags)337);
			}).Cast<WoWUnit>()
				.ToList();
			Dictionary<ulong, int> clusterCounts = precastTargets.ToDictionary((WoWUnit u) => ((WoWObject)u).Guid, (WoWUnit u) => precastTargets.Count((WoWUnit nearby) => ((WoWObject)nearby).Position.DistanceTo2D(((WoWObject)u).Position) <= 12.5f));
			WoWUnit val = (from u in precastTargets
				orderby clusterCounts[((WoWObject)u).Guid] descending, u.HealthPercent
				select u).FirstOrDefault();
			if (val == null)
			{
				val = (WoWUnit)ObjectManager.Me;
			}
			if (val != null)
			{
				if (((WoWUnit)ObjectManager.Me).Target != ((WoWObject)val).Guid)
				{
					SafeSetTarget(val);
					return true;
				}
				SpellManager.CastSpellByIdLUA(ResolveSpell("chain_heal"));
				return true;
			}
		}
		return false;
	}

    private enum TotemMatchStatus { MISSING, WRONG, MATCH, NOT_REQUIRED }

    private enum TotemSlotOwner
    {
        NONE,
        BASE,
        OVERRIDE,
        SPECIAL_DPS
    }

    private class TotemSlotStatus
    {
        public uint ExpectedId;
        public string ExpectedName;
        public string ExpectedNormalized;
        public int ExpectedRank;
        
        public uint ActualId;
        public string ActualName;
        public string ActualNormalized;
        public int ActualRank;
        
        public TotemMatchStatus Status;
        public string MatchMethod;
        public string RankCheck;
        public string ActualIdSource;
    }

    private string NormalizeTotemSelection(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "";
        raw = raw.Trim();
        if (raw.Equals("None", System.StringComparison.OrdinalIgnoreCase) || raw.Equals("Auto", System.StringComparison.OrdinalIgnoreCase)) return "";

        int metaIdx = raw.IndexOf("#||#");
        if (metaIdx >= 0)
        {
            raw = raw.Substring(0, metaIdx).Trim();
        }
        return raw;
    }

    private int ExtractTotemRank(string name)
    {
        if (string.IsNullOrEmpty(name)) return 0;
        string n = name.Trim().ToUpperInvariant();

        if (n.EndsWith(" X")) return 10;
        if (n.EndsWith(" IX")) return 9;
        if (n.EndsWith(" VIII")) return 8;
        if (n.EndsWith(" VII")) return 7;
        if (n.EndsWith(" VI")) return 6;
        if (n.EndsWith(" V")) return 5;
        if (n.EndsWith(" IV")) return 4;
        if (n.EndsWith(" III")) return 3;
        if (n.EndsWith(" II")) return 2;
        if (n.EndsWith(" I")) return 1;

        var match = System.Text.RegularExpressions.Regex.Match(n, @"(?:RANK|УРОВЕНЬ|РАНГ)\s*(\d+)");
        if (match.Success)
        {
            int rank = 0;
            if (int.TryParse(match.Groups[1].Value, out rank))
            {
                return rank;
            }
        }
        return 0;
    }

    private string NormalizeTotemName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "";
        
        int metaIdx = name.IndexOf("#||#");
        if (metaIdx >= 0) name = name.Substring(0, metaIdx);
        
        string n = name.Trim();
        
        int bracketIdx = n.IndexOf("(");
        if (bracketIdx > 0) n = n.Substring(0, bracketIdx).Trim();
        
        string[] roman = { " X", " IX", " VIII", " VII", " VI", " V", " IV", " III", " II", " I" };
        foreach (var r in roman)
        {
            if (n.EndsWith(r))
            {
                n = n.Substring(0, n.Length - r.Length).Trim();
                break;
            }
        }
        
        n = System.Text.RegularExpressions.Regex.Replace(n, @"(?i)(Уровень|Rank|Ранг)\s*\d+$", "").Trim();
        return n.ToLowerInvariant();
    }

    private TotemSlotStatus GetTotemSlotStatus(int slot, string rawConfigValue)
    {
        TotemSlotStatus s = new TotemSlotStatus();
        string normalizedSelection = NormalizeTotemSelection(rawConfigValue);
        s.ExpectedId = ResolveSpell(normalizedSelection);
        
        if (s.ExpectedId == 0)
        {
            s.ExpectedName = "None";
            s.ExpectedNormalized = "none";
            s.ExpectedRank = 0;
            s.ActualName = Lua.LuaDoString<string>("local have, name = GetTotemInfo(" + slot + "); if have and name ~= nil and name ~= '' then return name else return 'None' end", "");
            s.ActualNormalized = (s.ActualName == "None") ? "none" : NormalizeTotemName(s.ActualName);
            s.ActualRank = (s.ActualName == "None") ? 0 : ExtractTotemRank(s.ActualName);
            s.ActualId = 0;
            
            s.Status = TotemMatchStatus.NOT_REQUIRED;
            s.MatchMethod = "NONE";
            s.RankCheck = "NOT_REQUIRED";
            s.ActualIdSource = "NONE";
            return s;
        }

        s.ExpectedName = Lua.LuaDoString<string>("local name = GetSpellInfo(" + s.ExpectedId + "); return name or '';", "");
        s.ExpectedNormalized = NormalizeTotemName(s.ExpectedName);
        s.ExpectedRank = ExtractTotemRank(rawConfigValue);
        if (s.ExpectedRank == 0) s.ExpectedRank = ExtractTotemRank(s.ExpectedName);
        
        int luaRank = Lua.LuaDoString<int>("local n, r = GetSpellInfo(" + s.ExpectedId + "); if r then local d = string.match(r, '%d+'); if d then return tonumber(d) end end return 0;", "");
        if (luaRank > 0) s.ExpectedRank = luaRank;

        s.ActualName = Lua.LuaDoString<string>("local have, name = GetTotemInfo(" + slot + "); if have and name ~= nil and name ~= '' then return name else return 'None' end", "");
        
        if (s.ActualName == "None")
        {
            s.ActualNormalized = "none";
            s.ActualRank = 0;
            s.ActualId = 0;
            s.Status = TotemMatchStatus.MISSING;
            s.MatchMethod = "NONE";
            s.RankCheck = "UNAVAILABLE";
            s.ActualIdSource = "NONE";
            return s;
        }

        s.ActualNormalized = NormalizeTotemName(s.ActualName);
        s.ActualRank = ExtractTotemRank(s.ActualName);
        s.ActualId = 0; 
        
        s.ActualIdSource = "UNAVAILABLE"; // 3.3.5 GetTotemInfo does not provide raw ID



        if (s.ExpectedNormalized == s.ActualNormalized)
        {
            if (s.ExpectedRank > 0 && s.ActualRank > 0)
            {
                if (s.ExpectedRank == s.ActualRank)
                {
                    s.Status = TotemMatchStatus.MATCH;
                    s.MatchMethod = "NAME_RANK";
                    s.RankCheck = "PASS";
                }
                else
                {
                    s.Status = TotemMatchStatus.WRONG;
                    s.MatchMethod = "NAME_RANK";
                    s.RankCheck = "FAIL";
                }
            }
            else if (s.ExpectedRank == 0 && s.ActualRank == 0)
            {
                s.Status = TotemMatchStatus.MATCH;
                s.MatchMethod = "NAME_ONLY";
                s.RankCheck = "NOT_APPLICABLE";
            }
            else
            {
                s.Status = TotemMatchStatus.WRONG;
                s.MatchMethod = "NAME_RANK_MISMATCH";
                s.RankCheck = "FAIL";
            }
        }
        else
        {
            s.Status = TotemMatchStatus.WRONG;
            s.MatchMethod = "NAME_MISMATCH";
            s.RankCheck = "UNAVAILABLE";
        }
        
        return s;
    }
    private bool State_TotemCombatOverrides(ConfigCache c, bool isResto)
    {
        // Removed logic: Overrides are now natively tracked in State_TotemBasePreset.
        return false;
    }

    private class OverrideInfo
    {
        public bool Active;
        public uint SpellId;
        public string Name;
        public int Rank;
        public int Slot;
        public string MatchMethod;
    }

    private OverrideInfo GetActiveUtilityOverride(int slot)
    {
        OverrideInfo info = new OverrideInfo { Active = false, SpellId = 0, Name = "", Rank = 0, Slot = slot, MatchMethod = "NONE" };
        
        string actualName = Lua.LuaDoString<string>("local have, name = GetTotemInfo(" + slot + "); if have and name ~= nil and name ~= '' then return name else return 'None' end", "");
        if (actualName == "None") return info;

        string norm = NormalizeTotemName(actualName);
        int rank = ExtractTotemRank(actualName);

        uint[] checkSpells = new uint[0];
        if (slot == 2) checkSpells = new uint[] { 8143, 2484, 5730, 2062 };
        else if (slot == 3) checkSpells = new uint[] { 8170, 16190, 8166 };
        else if (slot == 4) checkSpells = new uint[] { 8177 };

        for (int i = 0; i < checkSpells.Length; i++)
        {
            string sName = Lua.LuaDoString<string>("return GetSpellInfo(" + checkSpells[i] + ") or ''", "");
            int sRank = Lua.LuaDoString<int>("local n, r = GetSpellInfo(" + checkSpells[i] + "); if r then local d = string.match(r, '%d+'); if d then return tonumber(d) end end return 0;", "");
            
            if (sName != "" && norm == NormalizeTotemName(sName))
            {
                if (sRank == 0 || sRank == rank)
                {
                    info.Active = true;
                    info.SpellId = checkSpells[i];
                    info.Name = actualName;
                    info.Rank = rank;
                    info.MatchMethod = "UTILITY_IDENTITY";
                    return info;
                }
            }
        }

        return info;
    }

    private bool CanProceedWithDps(TotemSlotStatus[] baseStatuses, TotemSlotOwner[] owners, out string dpsReason)
    {
        for (int i = 0; i < 4; i++)
        {
            if (owners[i] == TotemSlotOwner.OVERRIDE) continue;
            if (owners[i] == TotemSlotOwner.SPECIAL_DPS) continue;
            
            if (baseStatuses[i].Status == TotemMatchStatus.MISSING || baseStatuses[i].Status == TotemMatchStatus.WRONG)
            {
                dpsReason = "Base incomplete, no override";
                return false;
            }
        }
        dpsReason = "Base verified or overridden";
        return true;
    }

    private bool CanCallBaseTotems(uint callId, OverrideInfo[] overrides, TotemSlotOwner[] owners, out string callReason)
    {
        if (_callFailCount >= 1) { callReason = "Call failed previously"; return false; }
        if (callId == 0)
        {
            callReason = "Call unavailable";
            return false;
        }
        for (int i = 0; i < 4; i++)
        {
            if (overrides[i] != null && overrides[i].Active)
            {
                callReason = "UTILITY_OVERRIDE_ACTIVE";
                return false;
            }
            if (owners[i] == TotemSlotOwner.SPECIAL_DPS)
            {
                callReason = "SPECIAL_DPS_ACTIVE";
                return false;
            }
        }
        callReason = "NO_UTILITY_OVERRIDE";
        return true;
    }

    private bool IsBasePresetVerified(TotemSlotStatus[] baseStatuses)
    {
        for (int i = 0; i < 4; i++)
        {
            if (baseStatuses[i].Status == TotemMatchStatus.MISSING || baseStatuses[i].Status == TotemMatchStatus.WRONG)
            {
                return false;
            }
        }
        return true;
    }

    private bool State_TotemBasePreset(ConfigCache c, bool isResto)
    {
        uint eId = ResolveSpell(isResto ? c.Resto.ActiveEarthTotem : c.Ele.ActiveEarthTotem);
        uint fId = ResolveSpell(isResto ? c.Resto.ActiveFireTotem : c.Ele.ActiveFireTotem);
        uint wId = ResolveSpell(isResto ? c.Resto.ActiveWaterTotem : c.Ele.ActiveWaterTotem);
        uint aId = ResolveSpell(isResto ? c.Resto.ActiveAirTotem : c.Ele.ActiveAirTotem);
        uint callId = ResolveSpell("call_of_the_elements");

        bool inCombat = IsEffectiveCombat();
        bool moving = MovementManager.InMovement;

        if (eId != _lastSyncEarth || fId != _lastSyncFire || wId != _lastSyncWater || aId != _lastSyncAir)
        {
            _totemState = TotemPresetState.Dirty;
            _lastDpsPolicyAllow = false;
            _policyValid = false;
            _lastSyncEarth = eId; _lastSyncFire = fId; _lastSyncWater = wId; _lastSyncAir = aId;
            FTLine("[TOTEM MANAGER] PRESET CHANGED -> Dirty");
        }

        if (_totemState == TotemPresetState.Dirty)
        {
            if (!inCombat)
            {
                Lua.LuaDoString("if not InCombatLockdown() then SetMultiCastSpell(133, " + ((fId != 0) ? fId.ToString() : "0") + "); SetMultiCastSpell(134, " + ((eId != 0) ? eId.ToString() : "0") + "); SetMultiCastSpell(135, " + ((wId != 0) ? wId.ToString() : "0") + "); SetMultiCastSpell(136, " + ((aId != 0) ? aId.ToString() : "0") + "); end", false);
                _totemState = TotemPresetState.Synced;
                FTLine("[TOTEM MANAGER] OOC Action Bar Updated -> Synced");
            }
            else
            {
                _totemState = TotemPresetState.ReadyToCall;
                FTLine("[TOTEM MANAGER] InCombatLockdown -> ReadyToCall");
            }
            return false;
        }

        if (!inCombat) return false;

        if (_totemState == TotemPresetState.Synced)
        {
            _totemState = TotemPresetState.ReadyToCall;
            FTLine("[TOTEM MANAGER] Synced -> ReadyToCall");
            return false;
        }
        if (_totemState == TotemPresetState.Verified && Environment.TickCount < _nextTotemVerifyTime) return false;

        TotemSlotStatus fStat = GetTotemSlotStatus(1, isResto ? c.Resto.ActiveFireTotem : c.Ele.ActiveFireTotem);
        TotemSlotStatus eStat = GetTotemSlotStatus(2, isResto ? c.Resto.ActiveEarthTotem : c.Ele.ActiveEarthTotem);
        TotemSlotStatus wStat = GetTotemSlotStatus(3, isResto ? c.Resto.ActiveWaterTotem : c.Ele.ActiveWaterTotem);
        TotemSlotStatus aStat = GetTotemSlotStatus(4, isResto ? c.Resto.ActiveAirTotem : c.Ele.ActiveAirTotem);

        OverrideInfo oFire = new OverrideInfo { Active = false }; // Fire Ele handled below
        OverrideInfo oEarth = GetActiveUtilityOverride(2);
        OverrideInfo oWater = GetActiveUtilityOverride(3);
        OverrideInfo oAir = GetActiveUtilityOverride(4);
        
        string fireEleCanonical = NormalizeTotemName(Lua.LuaDoString<string>("return GetSpellInfo(2894) or ''", ""));
        bool isFireEleActive = (fStat.ActualNormalized != "" && fStat.ActualNormalized == fireEleCanonical);

        TotemSlotOwner fOwner = isFireEleActive ? TotemSlotOwner.SPECIAL_DPS : ((fStat.Status == TotemMatchStatus.MATCH || fStat.Status == TotemMatchStatus.NOT_REQUIRED) ? TotemSlotOwner.BASE : TotemSlotOwner.NONE);
        TotemSlotOwner eOwner = oEarth.Active ? TotemSlotOwner.OVERRIDE : ((eStat.Status == TotemMatchStatus.MATCH || eStat.Status == TotemMatchStatus.NOT_REQUIRED) ? TotemSlotOwner.BASE : TotemSlotOwner.NONE);
        TotemSlotOwner wOwner = oWater.Active ? TotemSlotOwner.OVERRIDE : ((wStat.Status == TotemMatchStatus.MATCH || wStat.Status == TotemMatchStatus.NOT_REQUIRED) ? TotemSlotOwner.BASE : TotemSlotOwner.NONE);
        TotemSlotOwner aOwner = oAir.Active ? TotemSlotOwner.OVERRIDE : ((aStat.Status == TotemMatchStatus.MATCH || aStat.Status == TotemMatchStatus.NOT_REQUIRED) ? TotemSlotOwner.BASE : TotemSlotOwner.NONE);

        TotemSlotStatus[] baseStatuses = new TotemSlotStatus[] { fStat, eStat, wStat, aStat };
        TotemSlotOwner[] owners = new TotemSlotOwner[] { fOwner, eOwner, wOwner, aOwner };
        OverrideInfo[] overrides = new OverrideInfo[] { oFire, oEarth, oWater, oAir };

        int overrideCount = 0;
        if (oEarth.Active) overrideCount++;
        if (oWater.Active) overrideCount++;
        if (oAir.Active) overrideCount++;

        string dpsReason;
        bool dpsAllowed = CanProceedWithDps(baseStatuses, owners, out dpsReason);
        _lastDpsPolicyAllow = dpsAllowed;
        _lastDpsReason = dpsReason;
        _policyTickId = _fsmTickId;
        _policyValid = true;
        _lastOverrideActive = overrideCount > 0;
        _lastSpecialDpsActive = (owners[0] == TotemSlotOwner.SPECIAL_DPS || owners[1] == TotemSlotOwner.SPECIAL_DPS || owners[2] == TotemSlotOwner.SPECIAL_DPS || owners[3] == TotemSlotOwner.SPECIAL_DPS);
        bool blockDps = !dpsAllowed;

        string callReason;
        bool callAllowed = CanCallBaseTotems(callId, overrides, owners, out callReason);

        string decision = "";
        string reason = "";

        if (_totemState == TotemPresetState.Called && Environment.TickCount >= _totemVerifyTime)
        {
            _totemState = TotemPresetState.ReadyToCall;
            if (_lastRestoreAction == TotemRestoreAction.GLOBAL_CALL) _callFailCount++;
            FTLine("[TOTEM STATE] Called verification timeout -> ReadyToCall");
        }

        bool baseVerified = IsBasePresetVerified(baseStatuses);
        _lastBaseVerified = baseVerified;

        if (_totemState == TotemPresetState.Verified)
        {
            if (!baseVerified)
            {
                _totemState = TotemPresetState.ReadyToCall;
                _lastRestoreAction = TotemRestoreAction.NONE;
                decision = "LOSS DETECTED";
                reason = "Base totem lost";
                FTLine("[TOTEM STATE] Verified -> LOSS DETECTED -> ReadyToCall");
            }
            else
            {
                decision = "VERIFIED";
                reason = "Base totems intact";
            }
        }

        if (_totemState == TotemPresetState.ReadyToCall)
        {
            if (baseVerified)
            {
                _totemState = TotemPresetState.Verified;
                _callFailCount = 0;
                _lastRestoreAction = TotemRestoreAction.NONE;
                decision = "STATE TRANSITION";
                reason = "Base totems restored";
                FTLine("[TOTEM STATE] ReadyToCall -> Verified");
            }
            else if (moving)
            {
                decision = "WAIT";
                reason = "Moving";
            }
            else if (callAllowed)
            {
                if (SpellManager.GetSpellCooldownTimeLeft(callId) <= 0)
                {
                    SpellManager.CastSpellByIdLUA(callId);
                    _totemState = TotemPresetState.Called;
                    _lastRestoreAction = TotemRestoreAction.GLOBAL_CALL;
                    _totemVerifyTime = (uint)Environment.TickCount + 1500;
                    decision = "CALL_OF_THE_ELEMENTS";
                    reason = "Missing base totems, casting global Call";
                    FTLine("\n[TOTEM RESTORE ACTION]\nACTION=GLOBAL_CALL\nSLOT=ALL\nSPELL=Call of the Elements\nREASON=" + reason + "\nFSM_STATE=Called\nBASE_VERIFIED=" + baseVerified + "\nDPS_RESULT=" + (dpsAllowed ? "ALLOW" : "BLOCK") + "\n");
                }
                else
                {
                    decision = "WAIT";
                    reason = "Call of the Elements on Cooldown";
                }
            }
            else
            {
                // Fallbacks
                if (fOwner == TotemSlotOwner.NONE && fStat.Status != TotemMatchStatus.NOT_REQUIRED && SpellManager.GetSpellCooldownTimeLeft(fId) <= 0) { SpellManager.CastSpellByIdLUA(fId); _totemState = TotemPresetState.Called; _lastRestoreAction = TotemRestoreAction.PARTIAL_FALLBACK; _totemVerifyTime = (uint)Environment.TickCount + 1500; decision = "FALLBACK CAST"; reason = "Fire"; FTLine("\n[TOTEM RESTORE ACTION]\nACTION=PARTIAL_FALLBACK\nSLOT=FIRE\nSPELL=" + fStat.ExpectedName + "\nREASON=Base Missing\nFSM_STATE=Called\nBASE_VERIFIED=" + baseVerified + "\nDPS_RESULT=" + (dpsAllowed ? "ALLOW" : "BLOCK") + "\n"); }
                else if (eOwner == TotemSlotOwner.NONE && eStat.Status != TotemMatchStatus.NOT_REQUIRED && SpellManager.GetSpellCooldownTimeLeft(eId) <= 0) { SpellManager.CastSpellByIdLUA(eId); _totemState = TotemPresetState.Called; _lastRestoreAction = TotemRestoreAction.PARTIAL_FALLBACK; _totemVerifyTime = (uint)Environment.TickCount + 1500; decision = "FALLBACK CAST"; reason = "Earth"; FTLine("\n[TOTEM RESTORE ACTION]\nACTION=PARTIAL_FALLBACK\nSLOT=EARTH\nSPELL=" + eStat.ExpectedName + "\nREASON=Base Missing\nFSM_STATE=Called\nBASE_VERIFIED=" + baseVerified + "\nDPS_RESULT=" + (dpsAllowed ? "ALLOW" : "BLOCK") + "\n"); }
                else if (wOwner == TotemSlotOwner.NONE && wStat.Status != TotemMatchStatus.NOT_REQUIRED && SpellManager.GetSpellCooldownTimeLeft(wId) <= 0) { SpellManager.CastSpellByIdLUA(wId); _totemState = TotemPresetState.Called; _lastRestoreAction = TotemRestoreAction.PARTIAL_FALLBACK; _totemVerifyTime = (uint)Environment.TickCount + 1500; decision = "FALLBACK CAST"; reason = "Water"; FTLine("\n[TOTEM RESTORE ACTION]\nACTION=PARTIAL_FALLBACK\nSLOT=WATER\nSPELL=" + wStat.ExpectedName + "\nREASON=Base Missing\nFSM_STATE=Called\nBASE_VERIFIED=" + baseVerified + "\nDPS_RESULT=" + (dpsAllowed ? "ALLOW" : "BLOCK") + "\n"); }
                else if (aOwner == TotemSlotOwner.NONE && aStat.Status != TotemMatchStatus.NOT_REQUIRED && SpellManager.GetSpellCooldownTimeLeft(aId) <= 0) { SpellManager.CastSpellByIdLUA(aId); _totemState = TotemPresetState.Called; _lastRestoreAction = TotemRestoreAction.PARTIAL_FALLBACK; _totemVerifyTime = (uint)Environment.TickCount + 1500; decision = "FALLBACK CAST"; reason = "Air"; FTLine("\n[TOTEM RESTORE ACTION]\nACTION=PARTIAL_FALLBACK\nSLOT=AIR\nSPELL=" + aStat.ExpectedName + "\nREASON=Base Missing\nFSM_STATE=Called\nBASE_VERIFIED=" + baseVerified + "\nDPS_RESULT=" + (dpsAllowed ? "ALLOW" : "BLOCK") + "\n"); }
                else
                {
                    if (overrideCount > 0)
                    {
                        decision = "DEFER";
                        reason = "ACTIVE_OVERRIDE";
                    }
                    else if (fOwner == TotemSlotOwner.SPECIAL_DPS)
                    {
                        decision = "DEFER";
                        reason = "SPECIAL_DPS_ACTIVE";
                    }
                    else
                    {
                        decision = "WAIT";
                        reason = "Base incomplete, fallbacks unavailable/cooldown";
                    }
                }
            }
        }
        else if (_totemState == TotemPresetState.Called)
        {
            decision = "WAIT_FOR_VERIFY";
            reason = "Waiting for API";
        }

        if (_totemState == TotemPresetState.Verified && !baseVerified)
        {
            FTLine("[FATAL FSM INVARIANT] Verified state with BaseVerified=false");
            _totemState = TotemPresetState.ReadyToCall;
            decision = "FATAL_RECOVERY";
            reason = "Invariant breach";
        }

        string trace = "[TOTEM OWNER]\n\n" +
            "FIRE:\nOWNER=" + fOwner.ToString() + "\nBASE_STATUS=" + fStat.Status.ToString() + "\nOVERRIDE=" + oFire.Name + "\nSPECIAL_DPS=" + (isFireEleActive ? fStat.ActualName : "false") + "\n\n" +
            "EARTH:\nOWNER=" + eOwner.ToString() + "\nBASE_STATUS=" + eStat.Status.ToString() + "\nOVERRIDE=" + (oEarth.Active ? oEarth.Name : "false") + "\n\n" +
            "WATER:\nOWNER=" + wOwner.ToString() + "\nBASE_STATUS=" + wStat.Status.ToString() + "\nOVERRIDE=" + (oWater.Active ? oWater.Name : "false") + "\n\n" +
            "AIR:\nOWNER=" + aOwner.ToString() + "\nBASE_STATUS=" + aStat.Status.ToString() + "\nOVERRIDE=" + (oAir.Active ? oAir.Name : "false") + "\n\n" +
            "[TOTEM CALL POLICY]\n\n" +
            "CALL_AVAILABLE=" + (callId != 0).ToString() + "\n" +
            "CALL_DECISION=" + (callAllowed ? "ALLOW" : "BLOCK") + "\n" +
            "CALL_REASON=" + callReason + "\n\n" +
            "[TOTEM FSM]\n\n" +
            "TOTEM_STATE=" + _totemState.ToString() + "\n" +
            "BASE_VERIFIED=" + baseVerified.ToString() + "\n" +
            "BASE_VALID=" + baseVerified.ToString() + "\n" +
            "OVERRIDE_ACTIVE=" + (overrideCount > 0).ToString() + "\n" +
            "OVERRIDE_SLOTS=" + (oEarth.Active ? "EARTH " : "") + (oWater.Active ? "WATER " : "") + (oAir.Active ? "AIR " : "") + "\n" +
            "SPECIAL_DPS=" + isFireEleActive.ToString() + "\n" +
            "DPS_RESULT=" + (dpsAllowed ? "ALLOW" : "BLOCK") + "\n" +
            "RESTORE_RESULT=" + (decision == "DEFER" ? "DEFER" : (decision == "CALL_OF_THE_ELEMENTS" || decision == "FALLBACK CAST" ? "ALLOW" : "NONE")) + "\n" +
            "RESTORE_ACTION=" + _lastRestoreAction.ToString() + "\n" +
            "FSM_DECISION=" + decision + "\n" +
            "FSM_REASON=" + reason;

        if (trace != _lastTotemTrace)
        {
            FTLine(trace);
            _lastTotemTrace = trace;
        }

        if (moving != _wasMoving && !moving)
        {
            _nextTotemVerifyTime = 0; 
        }
        _wasMoving = moving;

        return blockDps;
    }

    private string _lastTotemTrace = "";
    private uint _nextTotemVerifyTime = 0;
    private bool _wasMoving = false;
private bool State_BuffsAndTotems(ConfigCache c, bool isResto)
	{
		FT("[BUFFS FULL] State_BuffsAndTotems");
		if (((WoWUnit)ObjectManager.Me).IsMounted)
		{
			return false;
		}
		bool inCombatFlagOnly = IsEffectiveCombat();
		bool inMovement = MovementManager.InMovement || ((wManager.Wow.ObjectManager.WoWPlayer)ObjectManager.Me).GetMove;
		FTLine("combat=" + inCombatFlagOnly + " moving=" + inMovement);
		if (c.Common.TotemRecall && !inCombatFlagOnly && c.Common.TotemAliveTime > 0)
		{
			bool flag = Lua.LuaDoString<bool>("local maxTime = " + c.Common.TotemAliveTime + ";for i=1,4 do local have, _, startTime = GetTotemInfo(i); if have and startTime > 0 then local alive = GetTime() - startTime; if alive >= maxTime then return true end end end return false;", "");
			uint num = ResolveSpell("totemic_recall");
			if (flag && num != 0 && SpellManager.GetSpellCooldownTimeLeft(num) <= 0)
			{
				FTLine("ACTION CAST Totemic Recall");
				SpellManager.CastSpellByIdLUA(num);
				FTLine("RETURN TRUE: Buffs.TotemRecall");
				return true;
			}
		}
		string text = (isResto ? c.Resto.ActiveShield : c.Ele.ActiveShield);
		FTLine("CHECK Shield: " + text);
		if (!string.IsNullOrEmpty(text) && !HasExpectedState("Shield"))
		{
			uint num2 = ResolveSpell(text);
			bool flag2 = Lua.LuaDoString<bool>("return UnitBuff('player', GetSpellInfo(" + num2 + ")) ~= nil;", "");
			FTLine("sId=" + num2 + " hasShield=" + flag2);
			if (num2 != 0 && !flag2)
			{
				FTLine("ACTION CAST Shield " + num2);
				SpellManager.CastSpellByIdLUA(num2);
				AddExpectedState("Shield", 1500);
				FTLine("RETURN TRUE: Buffs.Shield");
				return true;
			}
		}
		string text2 = (isResto ? c.Resto.ActiveWeapon : c.Ele.ActiveWeapon);
		FTLine("CHECK Weapon: " + text2);
		if (!string.IsNullOrEmpty(text2) && !inMovement)
		{
			uint num3 = ResolveSpell(text2);
			FTLine("wepId=" + num3 + " expected=" + HasExpectedState("Weapon"));
			if (num3 != 0 && !HasExpectedState("Weapon"))
			{
				bool flag3 = Lua.LuaDoString<bool>("local h = GetWeaponEnchantInfo(); return h == true or h == 1;", "");
				float num4 = SpellManager.GetSpellCooldownTimeLeft(num3);
				FTLine("hasAnyWep=" + flag3 + " cd=" + num4);
				if (!flag3 && num4 <= 0f)
				{
					FTLine("ACTION CAST Weapon " + num3);
					SpellManager.CastSpellByIdLUA(num3);
					AddExpectedState("Weapon", 3000);
					FTLine("RETURN TRUE: Buffs.Weapon");
					return true;
				}
			}
		}
		if (inCombatFlagOnly && !inMovement)
		{
			FTLine("CHECK Saronite Bomb");
			if (c.Common.UseSaroniteBomb && ItemsManager.HasItemById(41119u) && Lua.LuaDoString<bool>("local _,d = GetItemCooldown(41119); return d == 0;", "") && !HasExpectedState("SaroniteBomb"))
			{
				WoWUnit target = ObjectManager.Target;
				if (target != null && ((WoWObject)target).IsValid && target.IsAttackable && ((WoWObject)target).GetDistance <= 30f)
				{
					FTLine("ACTION USE Saronite Bomb");
					ItemsManager.UseItem(41119u);
					ClickOnTerrain.Pulse(((WoWObject)target).Position);
					AddExpectedState("SaroniteBomb", 2000);
					FTLine("RETURN TRUE: Buffs.SaroniteBomb");
					return true;
				}
			}
		}
		FTLine("RESULT = FALSE -> CONTINUE");
		return false;
	}



	private void State_OffGcdBurst(ConfigCache c, ProcState procs)
	{
		WoWUnit combatTarget = GetCombatTarget();
		if (combatTarget == null || !IsBossLikeTarget(combatTarget))
		{
			return;
		}
		FT("[BURST CHECK] Off GCD Controller");
		if (c.Ele.UseElementalMastery && _caps.HasElementalMastery)
		{
			uint num = ResolveSpell("elemental_mastery");
			if (num != 0 && SpellManager.GetSpellCooldownTimeLeft(num) <= 0)
			{
				FT("[BURST ACTION] Elemental Mastery");
				SpellManager.CastSpellByIdLUA(num);
			}
		}
		if (c.Ele.UseRacial)
		{
			uint num2 = ResolveSpell("blood_fury");
			uint num3 = ResolveSpell("berserking");
			uint num4 = ((num2 != 0) ? num2 : num3);
			if (num4 != 0 && SpellManager.GetSpellCooldownTimeLeft(num4) <= 0)
			{
				FT("[BURST ACTION] Racial");
				SpellManager.CastSpellByIdLUA(num4);
			}
		}
		if (c.Ele.UseEngGloves && HasOffensiveGloveTinker() && Lua.LuaDoString<bool>("local s,d = GetInventoryItemCooldown('player', 10); return d == 0;", ""))
		{
			FT("[BURST ACTION] Engineering Gloves");
			Lua.LuaDoString("UseInventoryItem(10);", false);
		}
	}

	private void State_CoreRotation_Ele(ConfigCache c)
    {
        FT("[ELE FULL] State_CoreRotation_Ele");
        if (ObjectManager.Me.IsCast) { FTLine("RETURN: IS CASTING"); return; }
        
        WoWUnit currentTarget = ObjectManager.Target;
        WoWUnit combatTarget = GetCombatTarget(30f);
        
        if (combatTarget == null)
        {
            FTLine("RETURN: NO TARGET");
            return;
        }
        
        if (currentTarget == null || currentTarget.Guid != combatTarget.Guid)
        {
            FTLine("[TARGET SWITCH] FSMTick=" + _fsmTickId + " FROM=" + (currentTarget != null ? currentTarget.Name : "null") + " TO=" + combatTarget.Name + " REASON=Fallback Synchronization");
            SafeSetTarget(combatTarget);
            FTLine("RETURN: Target Sync");
            return;
        }
        
        bool moving = MovementManager.InMovement || ((wManager.Wow.ObjectManager.WoWPlayer)ObjectManager.Me).GetMove;
        // EMERGENCY SELF HEAL (From AIO)
        if (ObjectManager.Me.HealthPercent < 50)
        {
            uint lhwId = ResolveSpell("lesser_healing_wave");
            if (lhwId != 0 && SpellManager.GetSpellCooldownTimeLeft(lhwId) <= 0)
            {
                SpellManager.CastSpellByIdLUA(lhwId);
                FTLine("RETURN: EMERGENCY HEAL");
                return;
            }
        }

        // CURE TOXINS (From AIO)
        if (ObjectManager.Me.HaveBuff("Poison") || ObjectManager.Me.HaveBuff("Disease"))
        {
            uint cureId = ResolveSpell("cure_toxins");
            if (cureId != 0 && SpellManager.GetSpellCooldownTimeLeft(cureId) <= 0)
            {
                SpellManager.CastSpellByIdLUA(cureId);
                FTLine("RETURN: CURE TOXINS");
                return;
            }
        }


        

        // ---------------------------------------------------------
        // OFF-GCD BURST COORDINATOR (ATOMIC MACRO)
        // ---------------------------------------------------------
        bool bloodlustActive = BuffManager.HaveBuff(((WoWObject)ObjectManager.Me).GetBaseAddress, ResolveSpell("bloodlust")) || BuffManager.HaveBuff(((WoWObject)ObjectManager.Me).GetBaseAddress, ResolveSpell("heroism"));
        bool burstPhase = IsBossLikeTarget(combatTarget) || bloodlustActive;

        if (burstPhase)
        {
            if (c.Ele.UseTrinket1 && !HasExpectedState("Trinket1") && Lua.LuaDoString<bool>("local s, d, e = GetInventoryItemCooldown('player', 13); return s == 0;", ""))
            {
                Lua.LuaDoString("UseInventoryItem(13);", false);
                AddExpectedState("Trinket1", 2000);
                FTLine("[ELE BURST] Trinket 1 Used");
            }

            if (c.Ele.UseTrinket2 && !HasExpectedState("Trinket2") && Lua.LuaDoString<bool>("local s, d, e = GetInventoryItemCooldown('player', 14); return s == 0;", ""))
            {
                Lua.LuaDoString("UseInventoryItem(14);", false);
                AddExpectedState("Trinket2", 2000);
                FTLine("[ELE BURST] Trinket 2 Used");
            }

            if (c.Ele.UseEngGloves && !HasExpectedState("EngGloves") && Lua.LuaDoString<bool>("local s, d, e = GetInventoryItemCooldown('player', 10); return s == 0;", ""))
            {
                Lua.LuaDoString("UseInventoryItem(10);", false);
                AddExpectedState("EngGloves", 2000);
                FTLine("[ELE BURST] Gloves Used");
            }

            if (c.Ele.UseRacial && !HasExpectedState("Racial"))
            {
                uint bloodFuryId = ResolveSpell("blood_fury");
                uint berserkingId = ResolveSpell("berserking");
                
                if (bloodFuryId != 0 && SpellManager.GetSpellCooldownTimeLeft(bloodFuryId) <= 0)
                {
                    SpellManager.CastSpellByIdLUA(bloodFuryId);
                    AddExpectedState("Racial", 2000);
                    FTLine("[ELE BURST] Blood Fury Used");
                }
                else if (berserkingId != 0 && SpellManager.GetSpellCooldownTimeLeft(berserkingId) <= 0)
                {
                    SpellManager.CastSpellByIdLUA(berserkingId);
                    AddExpectedState("Racial", 2000);
                    FTLine("[ELE BURST] Berserking Used");
                }
            }

            if (c.Ele.UseElementalMastery && !HasExpectedState("EM"))
            {
                uint emId = ResolveSpell("elemental_mastery");
                if (emId != 0 && SpellManager.GetSpellCooldownTimeLeft(emId) <= 0)
                {
                    SpellManager.CastSpellByIdLUA(emId);
                    AddExpectedState("EM", 2000);
                    FTLine("[ELE BURST] Elemental Mastery Used");
                }
            }
        }
        // Fire Elemental (Burst Layer)
        uint fireEleId = ResolveSpell("fire_elemental_totem");
        if (c.Ele.UseFireElemental && fireEleId != 0 && IsBossLikeTarget(combatTarget) && SpellManager.GetSpellCooldownTimeLeft(fireEleId) <= 0 && !HasExpectedState("FireEle"))
        {
            ProcState procState = default(ProcState);
            procState.Update();
            bool goodSnapshot = procState.SnapshotScore >= 500f || procState.ActiveProcsCount >= 2;
            if (_fireEleWaitStart == 0L) _fireEleWaitStart = Environment.TickCount;
            bool timeOut = Environment.TickCount - _fireEleWaitStart > 15000;
            
            FTLine("FireEle Snapshot: score=" + procState.SnapshotScore + " count=" + procState.ActiveProcsCount + " wait=" + (Environment.TickCount - _fireEleWaitStart));
            if (goodSnapshot || timeOut)
            {
                FTLine("ACTION CAST FireEle");
                SpellManager.CastSpellByIdLUA(fireEleId);
                AddExpectedState("FireEle", 2000);
                _fireEleWaitStart = 0L;
                FTLine("RETURN TRUE: ELE.FireEle");
                return;
            }
            FTLine("REASON=Waiting for better snapshot");
            return; // Wait for snapshot!
        }
        else
        {
            _fireEleWaitStart = 0L;
        }
        
        // ---------------------------------------------------------
        // Priority 0: Thunderstorm (MANA RECOVERY OVERRIDE)
        // ---------------------------------------------------------
        uint tsPri0Id = ResolveSpell("thunderstorm");
        bool tsPri0Known = tsPri0Id > 0;
        float tsPri0Cd = tsPri0Known ? SpellManager.GetSpellCooldownTimeLeft(tsPri0Id) : -1f;
        bool tsPri0Blocked = HasExpectedState("Thunderstorm_Attempt");

        bool tsPri0ManaEligible = c.Ele.ThunderstormMana > 0 && ObjectManager.Me.ManaPercentage <= c.Ele.ThunderstormMana;
        
        if (tsPri0Known && c.Ele.UseThunderstorm && tsPri0ManaEligible && tsPri0Cd <= 0 && !tsPri0Blocked)
        {
            FTLine("[THUNDERSTORM MANA OVERRIDE] MANA=" + ObjectManager.Me.ManaPercentage + " THRESHOLD=" + c.Ele.ThunderstormMana + " CD=" + tsPri0Cd.ToString("0") + "ms");
            SpellManager.CastSpellByIdLUA(tsPri0Id);
            AddExpectedState("Thunderstorm_Attempt", 1500); // Instant cast, GCD + latency
            FTLine("RETURN TRUE: ELE.Thunderstorm (Mana)");
            return;
        }

        // ---------------------------------------------------------
		// Flame Shock
		uint fsId = ResolveSpell("flame_shock");
		bool fsKnown = fsId > 0;
		float fsCd = fsKnown ? SpellManager.GetSpellCooldownTimeLeft(fsId) : -1f;

		bool hasFs = false;
		float fsDuration = 0f;
		if (fsKnown && combatTarget != null && ((WoWObject)combatTarget).IsValid)
		{
			wManager.Wow.Class.Aura fsAura = wManager.Wow.Helpers.BuffManager.GetAuras(((WoWObject)combatTarget).GetBaseAddress)
				.FirstOrDefault(a => a.SpellId == fsId && a.Owner == ((WoWObject)ObjectManager.Me).Guid);
			if (fsAura != null)
			{
				hasFs = true;
				fsDuration = (float)(fsAura.TimeLeft) / 1000f; // Convert ms to seconds
			}
		}

		float fsThreshold = 2.0f; // Configurable boundary
		bool needFs = fsKnown && fsCd <= 0 && (!hasFs || fsDuration < fsThreshold);

		// Anti-spam check for Immune/LOS failures
		bool isFsBlocked = HasExpectedState("FlameShock_Attempt");

		FTLine("[FLAME SHOCK] Target=" + combatTarget.Name + " HasFS=" + hasFs + " Remaining=" + fsDuration.ToString("0.0") + "s Threshold=" + fsThreshold.ToString("0.0") + "s NeedFS=" + needFs + " CD=" + fsCd + "ms Blocked=" + isFsBlocked);

		if (c.Ele.UseFlameShock && needFs && !isFsBlocked)
		{
			FTLine("[FLAME SHOCK CAST] Target=" + combatTarget.Name + " REASON=" + (!hasFs ? "Missing" : "Refresh Window"));
			SpellManager.CastSpellByIdLUA(fsId);
			AddExpectedState("FlameShock_Attempt", 1500); // 1.5s Anti-Spam protection
			FTLine("RETURN TRUE: ELE.FlameShock");
			return;
		}
        
        // Lava Burst
        uint lvbId = ResolveSpell("lava_burst");
        bool lvbKnown = lvbId > 0;
        float lvbCd = lvbKnown ? SpellManager.GetSpellCooldownTimeLeft(lvbId) : -1f;

        // Lava Burst is a hard cast in 3.3.5a (no Lava Surge proc exists yet)
        // Can only be cast while standing still
        bool lvbEligible = c.Ele.UseLavaBurst && lvbKnown && lvbCd <= 0 && hasFs && !moving;
        bool isLvbBlocked = HasExpectedState("LavaBurst_Attempt");

        FTLine("[LAVA BURST] Target=" + combatTarget.Name
            + " FS=" + hasFs + " FS_Rem=" + fsDuration.ToString("0.0") + "s"
            + " CD=" + lvbCd.ToString("0") + "ms"
            + " Moving=" + moving
            + " Eligible=" + lvbEligible + " Blocked=" + isLvbBlocked);

        if (lvbEligible && !isLvbBlocked)
        {
            FTLine("[LAVA BURST CAST] Target=" + combatTarget.Name + " REASON=Normal");
            SpellManager.CastSpellByIdLUA(lvbId);
            AddExpectedState("LavaBurst_Attempt", 2500); // 2s cast + net latency
            FTLine("RETURN TRUE: ELE.LavaBurst");
            return;
        }
        else if (lvbKnown && lvbCd <= 0 && hasFs && !lvbEligible && !isLvbBlocked)
        {
            FTLine("[LAVA BURST BLOCK] Target=" + combatTarget.Name
                + " REASON=" + (!c.Ele.UseLavaBurst ? "Disabled" : moving ? "Moving" : !hasFs ? "NoFlameShock" : "Unknown"));
        }

        
        // ---------------------------------------------------------
        // =========================================================
        // CANONICAL AOE ENGINE (Task #11)
        // =========================================================
        Vector3 playerPos = ((WoWObject)ObjectManager.Me).Position;
        Vector3 targetPos = combatTarget != null ? ((WoWObject)combatTarget).Position : playerPos;
        bool hasTarget = combatTarget != null;

        int enemiesAroundPlayer = 0;
        int enemiesAroundTarget = 0;

        // Single deterministic pass over valid hostile units
        foreach (WoWUnit u in ObjectManager.GetWoWUnitHostile())
        {
            if (u.IsAlive && u.IsAttackable && u.InCombatFlagOnly)
            {
                float distToPlayer = ((WoWObject)u).Position.DistanceTo(playerPos);
                if (distToPlayer <= 10f)
                {
                    enemiesAroundPlayer++;
                }

                if (hasTarget && u.Guid != combatTarget.Guid)
                {
                    float distToTarget = ((WoWObject)u).Position.DistanceTo(targetPos);
                    if (distToTarget <= 10f)
                    {
                        enemiesAroundTarget++;
                    }
                }
            }
        }

        int fnTotalTargets = enemiesAroundPlayer; 
        int tsTotalTargets = enemiesAroundPlayer; 
        int clTotalTargets = hasTarget ? (1 + enemiesAroundTarget) : 0;
        int aoeThreshold = 3; // Baseline threshold for AOE logic

        FTLine("[AOE ENGINE] PlayerEnemies(10y)=" + enemiesAroundPlayer
            + " TargetEnemies(10y)=" + enemiesAroundTarget
            + " CL_Total=" + clTotalTargets
            + " THRESHOLD=" + aoeThreshold);

        // ---------------------------------------------------------
        // ---------------------------------------------------------
        // Priority 3: Fire Nova (AOE)
        // ---------------------------------------------------------
        uint fnId = ResolveSpell("fire_nova");
        bool fnKnown = fnId > 0;
        float fnCd = fnKnown ? SpellManager.GetSpellCooldownTimeLeft(fnId) : -1f;

        bool hasFireTotem = false;
        string fireTotemName = "None";
        if (fnKnown)
        {
            hasFireTotem = Lua.LuaDoString<bool>("local have, name, start, dur = GetTotemInfo(1); return have and name ~= nil and name ~= '' and dur > 0;", "");
            if (hasFireTotem) {
                fireTotemName = Lua.LuaDoString<string>("local have, name = GetTotemInfo(1); return name or 'Unknown';", "");
            }
        }
        
        bool fnAoeEligible = (fnTotalTargets >= aoeThreshold);
        bool isFnBlocked = HasExpectedState("FireNova_Attempt");
        
        bool fnEligible = fnKnown && fnCd <= 0 && hasFireTotem && fnAoeEligible && !isFnBlocked;

        if (fnKnown || fnTotalTargets >= aoeThreshold)
        {
            FTLine("[FIRE NOVA] FIRE_TOTEM=" + hasFireTotem + " (" + fireTotemName + ")"
                + " PLAYER_ENEMIES=" + fnTotalTargets + " THRESHOLD=" + aoeThreshold
                + " CD=" + fnCd.ToString("0") + "ms"
                + " Eligible=" + fnEligible + " Blocked=" + isFnBlocked);
        }

        if (fnEligible)
        {
            FTLine("[FIRE NOVA CAST] FIRE_TOTEM=" + fireTotemName + " ENEMY_COUNT=" + fnTotalTargets + " REASON=AOE");
            SpellManager.CastSpellByIdLUA(fnId);
            AddExpectedState("FireNova_Attempt", 1500); // Instant cast, GCD + latency
            FTLine("RETURN TRUE: ELE.FireNova");
            return;
        }
        else if (fnKnown && fnTotalTargets >= aoeThreshold && fnCd <= 0 && !isFnBlocked)
        {
            string fnReason = !hasFireTotem ? "NoFireTotem" : "Unknown";
            FTLine("[FIRE NOVA BLOCK] REASON=" + fnReason);
        }
        // Priority 4: Chain Lightning (AOE or ST)
        // ---------------------------------------------------------
        uint clId = ResolveSpell("chain_lightning");
        bool clKnown = clId > 0;
        float clCd = clKnown ? SpellManager.GetSpellCooldownTimeLeft(clId) : -1f;
        
        bool clIsAoeEligible = (clTotalTargets >= aoeThreshold);
        bool clIsStEligible = (!c.Ele.ChainLightningAoe && ObjectManager.Me.ManaPercentage > 60);
        
        bool clEligible = c.Ele.UseChainLightning && clKnown && clCd <= 0 && !moving && (clIsAoeEligible || clIsStEligible);
        bool isClBlocked = HasExpectedState("ChainLightning_Attempt");

        FTLine("[CHAIN LIGHTNING] Target=" + combatTarget.Name
            + " CL_Targets=" + clTotalTargets + " Threshold=" + aoeThreshold
            + " CD=" + clCd.ToString("0") + "ms" + " Moving=" + moving
            + " Mana=" + ObjectManager.Me.ManaPercentage + "%"
            + " Eligible=" + clEligible + " Blocked=" + isClBlocked);

        if (clEligible && !isClBlocked)
        {
            FTLine("[CHAIN LIGHTNING CAST] Target=" + combatTarget.Name 
                + " ENEMY_COUNT=" + clTotalTargets 
                + " REASON=" + (clIsAoeEligible ? "AOE" : "ST"));
            SpellManager.CastSpellByIdLUA(clId);
            AddExpectedState("ChainLightning_Attempt", 2500); // 2s cast base + latency
            FTLine("RETURN TRUE: ELE.ChainLightning");
            return;
        }
        else if (clKnown && clCd <= 0 && !clEligible && !isClBlocked)
        {
            string clReason = !c.Ele.UseChainLightning ? "Disabled" : 
                              moving ? "Moving" : 
                              (c.Ele.ChainLightningAoe && !clIsAoeEligible) ? "AoeCountNotMet" : 
                              (!clIsAoeEligible && ObjectManager.Me.ManaPercentage <= 60) ? "LowManaForST" : "Unknown";
            FTLine("[CHAIN LIGHTNING BLOCK] Target=" + combatTarget.Name + " REASON=" + clReason);
        }

        // ---------------------------------------------------------
        // ---------------------------------------------------------
        // Priority 5: Thunderstorm (AOE / MANA)
        // ---------------------------------------------------------
        uint tsId = ResolveSpell("thunderstorm");
        bool tsKnown = tsId > 0;
        float tsCd = tsKnown ? SpellManager.GetSpellCooldownTimeLeft(tsId) : -1f;

        bool tsBlocked = HasExpectedState("Thunderstorm_Attempt");
        
        bool tsAoeEligible = c.Ele.ThunderstormAoe && (tsTotalTargets >= aoeThreshold);
        
        bool tsEligible = c.Ele.UseThunderstorm && tsKnown && tsCd <= 0 && !tsBlocked && (tsAoeEligible);

        if (tsKnown || tsAoeEligible)
        {
            FTLine("[THUNDERSTORM] PLAYER_ENEMIES=" + tsTotalTargets + " THRESHOLD=" + aoeThreshold
                + " MANA=" + ObjectManager.Me.ManaPercentage + " MANA_THRESHOLD=" + c.Ele.ThunderstormMana
                + " AOE_FLAG=" + c.Ele.ThunderstormAoe + " CD=" + tsCd.ToString("0") + "ms"
                + " Eligible=" + tsEligible + " Blocked=" + tsBlocked);
        }

        if (tsEligible)
        {
            string tsReason = "AOE";
            FTLine("[THUNDERSTORM CAST] ENEMY_COUNT=" + tsTotalTargets + " MANA=" + ObjectManager.Me.ManaPercentage + " REASON=" + tsReason);
            SpellManager.CastSpellByIdLUA(tsId);
            AddExpectedState("Thunderstorm_Attempt", 1500); // Instant cast, GCD + latency
            FTLine("RETURN TRUE: ELE.Thunderstorm");
            return;
        }
        else if (tsKnown && tsAoeEligible && tsCd <= 0 && !tsBlocked)
        {
            FTLine("[THUNDERSTORM BLOCK] REASON=DisabledByConfig");
        }
        // Priority 6: Lightning Bolt (Filler)
        // ---------------------------------------------------------
        uint lbId = ResolveSpell("lightning_bolt");
        bool lbKnown = lbId > 0;
        bool lbEligible = c.Ele.UseLightningBolt && lbKnown && !moving;
        bool isLbBlocked = HasExpectedState("LightningBolt_Attempt");

        FTLine("[LIGHTNING BOLT] Target=" + combatTarget.Name
            + " Moving=" + moving
            + " Eligible=" + lbEligible + " Blocked=" + isLbBlocked);

        if (lbEligible && !isLbBlocked)
        {
            FTLine("[LIGHTNING BOLT CAST] Target=" + combatTarget.Name + " REASON=Filler");
            SpellManager.CastSpellByIdLUA(lbId);
            AddExpectedState("LightningBolt_Attempt", 2500); // 2.5s base (reduced by talents/haste) + latency
            FTLine("RETURN TRUE: ELE.LightningBolt");
            return;
        }
        else if (lbKnown && !lbEligible && !isLbBlocked)
        {
            FTLine("[LIGHTNING BOLT BLOCK] Target=" + combatTarget.Name
                + " REASON=" + (!c.Ele.UseLightningBolt ? "Disabled" : moving ? "Moving" : "Unknown"));
        }

        // Earth Shock (Movement Fallback)
        if (moving)
        {
            uint esId = ResolveSpell("earth_shock");
            float esCd = esId > 0 ? SpellManager.GetSpellCooldownTimeLeft(esId) : -1f;
            bool esEligible = c.Ele.UseEarthShock && esId > 0 && esCd <= 0;
            
            FTLine("[EARTH SHOCK] TARGET=" + combatTarget.Name + " MOVING=" + moving + " ES_COOLDOWN=" + esCd + " ELIGIBLE=" + esEligible);
            
            if (esEligible)
            {
                FTLine("[EARTH SHOCK CAST] TARGET=" + combatTarget.Name + " MAELSTROM=NONE REASON=Movement");
                SpellManager.CastSpellByIdLUA(esId);
                AddExpectedState("EarthShock", 1500);
                FTLine("RETURN TRUE: ELE.EarthShock");
                return;
            }
            else
            {
                FTLine("[EARTH SHOCK BLOCK] REASON=" + (!c.Ele.UseEarthShock ? "Disabled" : "Cooldown"));
            }
        }
        


        // Frost Shock (Movement Fallback)
        if (moving)
        {
            uint frsId = ResolveSpell("frost_shock");
            float frsCd = frsId > 0 ? SpellManager.GetSpellCooldownTimeLeft(frsId) : -1f;
            bool frsEligible = c.Ele.UseFrostShock && frsId > 0 && frsCd <= 0;
            
            FTLine("[FROST SHOCK] TARGET=" + combatTarget.Name + " MOVING=" + moving + " FRS_COOLDOWN=" + frsCd + " ELIGIBLE=" + frsEligible);
            
            if (frsEligible)
            {
                FTLine("[FROST SHOCK CAST] TARGET=" + combatTarget.Name + " REASON=Movement");
                SpellManager.CastSpellByIdLUA(frsId);
                AddExpectedState("FrostShock", 1500);
                FTLine("RETURN TRUE: ELE.FrostShock");
                return;
            }
            else
            {
                FTLine("[FROST SHOCK BLOCK] REASON=" + (!c.Ele.UseFrostShock ? "Disabled" : "Cooldown"));
            }
        }
        FTLine("RESULT = NO SPELL CAST");
    }

	private bool State_CoreRotation_Resto(ConfigCache c)
	{
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Expected O, but got Unknown
		List<WoWUnit> list = ObjectManager.GetObjectWoWPlayer().Where(delegate(WoWPlayer u)
		{
			//IL_000a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Invalid comparison between Unknown and I4
			return ((WoWUnit)u).IsAlive && (int)((WoWUnit)u).Reaction >= 4 && ((WoWObject)u).GetDistance <= 40f && !TraceLine.TraceLineGo(((WoWObject)ObjectManager.Me).Position, ((WoWObject)u).Position, (CGWorldFrameHitFlags)337);
		}).Cast<WoWUnit>()
			.ToList();
		if (c.Resto.ValithriaEnable)
		{
			WoWUnit val = ObjectManager.GetObjectWoWUnit().FirstOrDefault((WoWUnit u) => ((WoWObject)u).Entry == 36789 && u.IsAlive && ((WoWObject)u).GetDistance <= 40f && !TraceLine.TraceLineGo(((WoWObject)ObjectManager.Me).Position, ((WoWObject)u).Position, (CGWorldFrameHitFlags)337));
			if (val != null)
			{
				list.Add(val);
			}
		}
		list.Add((WoWUnit)ObjectManager.Me);
		list = list.Distinct().ToList();
		if (list.Count == 0)
		{
			return false;
		}
		TrackHealth(list);
		float num = 100f;
		foreach (WoWUnit item in list)
		{
			if (item.HealthPercent < (double)num)
			{
				num = (float)item.HealthPercent;
			}
		}
		WoWUnit tank = null;
		int num2 = -1;
		foreach (WoWUnit item2 in list)
		{
			int tankScore = GetTankScore(item2, c);
			if (tankScore > num2)
			{
				num2 = tankScore;
				tank = item2;
			}
		}
		if (c.Resto.EarthShieldRefresh && tank != null && !HasExpectedState("EarthShield"))
		{
			bool flag = true;
			Aura val2 = BuffManager.GetAuras(((WoWObject)tank).GetBaseAddress).FirstOrDefault((Aura a) => a.SpellId == 49284);
			if (val2 != null && val2.TimeLeft > c.Resto.EarthShieldRefreshThresholdMs)
			{
				flag = false;
			}
			if (flag && ResolveSpell("earth_shield") != 0 && SpellManager.GetSpellCooldownTimeLeft(ResolveSpell("earth_shield")) <= 0)
			{
				if (((WoWUnit)ObjectManager.Me).Target != ((WoWObject)tank).Guid)
				{
					SafeSetTarget(tank);
					return true;
				}
				SpellManager.CastSpellByIdLUA(ResolveSpell("earth_shield"));
				AddExpectedState("EarthShield", 2000);
				return true;
			}
		}
		bool flag2 = c.Resto.ValithriaEnable && list.Any((WoWUnit x) => ((WoWObject)x).Entry == 36789 && x.HealthPercent < 100.0);
		if (list.Count == 0)
		{
			return false;
		}
		if (num < 65f || flag2)
		{
			if (c.Resto.UseRacial && !HasExpectedState("Racial"))
			{
				uint num3 = ResolveSpell("blood_fury");
				uint num4 = ResolveSpell("berserking");
				if (num3 != 0 && SpellManager.GetSpellCooldownTimeLeft(num3) <= 0)
				{
					SpellManager.CastSpellByIdLUA(num3);
					AddExpectedState("Racial", 120000);
					Logging.Write("[RESTO OFFGCD] Blood Fury (" + num3 + ")");
					return true;
				}
				if (num4 != 0 && SpellManager.GetSpellCooldownTimeLeft(num4) <= 0)
				{
					SpellManager.CastSpellByIdLUA(num4);
					AddExpectedState("Racial", 180000);
					Logging.Write("[RESTO OFFGCD] Berserking (" + num4 + ")");
					return true;
				}
			}
			if (c.Resto.UseTrinket1 && !HasExpectedState("T1") && Lua.LuaDoString<bool>("local _,d,_=GetInventoryItemCooldown('player',13); return d==0;", ""))
			{
				Lua.LuaDoString("UseInventoryItem(13);", false);
				AddExpectedState("T1", 120000);
				Logging.Write("[RESTO OFFGCD] Trinket 1");
				return true;
			}
			if (c.Resto.UseTrinket2 && !HasExpectedState("T2") && Lua.LuaDoString<bool>("local _,d,_=GetInventoryItemCooldown('player',14); return d==0;", ""))
			{
				Lua.LuaDoString("UseInventoryItem(14);", false);
				AddExpectedState("T2", 120000);
				Logging.Write("[RESTO OFFGCD] Trinket 2");
				return true;
			}
			if (c.Resto.UseEngGloves && !HasExpectedState("Gloves") && Lua.LuaDoString<bool>("local _,d,_=GetInventoryItemCooldown('player',10); return d==0;", ""))
			{
				Lua.LuaDoString("UseInventoryItem(10);", false);
				AddExpectedState("Gloves", 60000);
				Logging.Write("[RESTO OFFGCD] Eng Gloves");
				return true;
			}
		}
		if (_panicState != PanicState.None)
		{
			if ((uint)(Environment.TickCount - (int)_panicDeadline) < 2147483648u)
			{
				_panicState = PanicState.None;
				Logging.Write("[RESTO PANIC] Sequence expired or interrupted");
			}
			else
			{
				WoWUnit val3 = list.FirstOrDefault((WoWUnit x) => ((WoWObject)x).Guid == _panicTargetGuid);
				if (val3 != null && val3.IsAlive && val3.HealthPercent < 40.0)
				{
					if (((WoWUnit)ObjectManager.Me).Target != ((WoWObject)val3).Guid)
					{
						SafeSetTarget(val3);
						return true;
					}
					if (_panicState == PanicState.CastNS)
					{
						if (c.Resto.UseNaturesSwiftness && ResolveSpell("natures_swiftness") != 0 && !Lua.LuaDoString<bool>("return UnitBuff('player', GetSpellInfo(" + ResolveSpell("natures_swiftness") + ")) ~= nil;", ""))
						{
							if (SpellManager.GetSpellCooldownTimeLeft(ResolveSpell("natures_swiftness")) <= 0)
							{
								SpellManager.CastSpellByIdLUA(ResolveSpell("natures_swiftness"));
							}
							return true;
						}
						_panicState = PanicState.CastTidal;
					}
					if (_panicState == PanicState.CastTidal)
					{
						if (c.Resto.UseNaturesSwiftness && ResolveSpell("tidal_force") != 0 && !Lua.LuaDoString<bool>("return UnitBuff('player', GetSpellInfo(" + ResolveSpell("tidal_force") + ")) ~= nil;", ""))
						{
							if (SpellManager.GetSpellCooldownTimeLeft(ResolveSpell("tidal_force")) <= 0)
							{
								SpellManager.CastSpellByIdLUA(ResolveSpell("tidal_force"));
							}
							return true;
						}
						_panicState = PanicState.CastHW;
					}
					if (_panicState == PanicState.CastHW)
					{
						if (c.Resto.UseHealingWave && ResolveSpell("healing_wave") != 0)
						{
							SpellManager.CastSpellByIdLUA(ResolveSpell("healing_wave"));
						}
						_panicState = PanicState.None;
						return true;
					}
				}
				else
				{
					_panicState = PanicState.None;
				}
			}
		}
		List<WoWUnit> list2 = (from val8 in list
			where val8.HealthPercent < 20.0
			orderby val8.HealthPercent - (double)((tank != null && ((WoWObject)val8).Guid == ((WoWObject)tank).Guid) ? 30f : 0f)
			select val8).ToList();
		if (list2.Count > 0 && _panicState == PanicState.None)
		{
			WoWUnit val4 = list2.First();
			if (c.Resto.UseNaturesSwiftness && ResolveSpell("natures_swiftness") != 0 && SpellManager.GetSpellCooldownTimeLeft(ResolveSpell("natures_swiftness")) <= 0)
			{
				_panicState = PanicState.CastNS;
				_panicTargetGuid = ((WoWObject)val4).Guid;
				_panicDeadline = (uint)(Environment.TickCount + 3000);
				Logging.Write("[RESTO PANIC] Initiating NS Sequence on " + ((WoWObject)val4).Name);
				if (((WoWUnit)ObjectManager.Me).Target != ((WoWObject)val4).Guid)
				{
					SafeSetTarget(val4);
					return true;
				}
				return true;
			}
		}
		if (flag2 && c.Resto.UseHealingWave && ResolveSpell("healing_wave") != 0 && !MovementManager.InMovement)
		{
			WoWUnit val5 = list.FirstOrDefault((WoWUnit x) => ((WoWObject)x).Entry == 36789 && x.HealthPercent < 100.0);
			if (val5 != null)
			{
				if (((WoWUnit)ObjectManager.Me).Target != ((WoWObject)val5).Guid)
				{
					SafeSetTarget(val5);
					return true;
				}
				SpellManager.CastSpellByIdLUA(ResolveSpell("healing_wave"));
				return true;
			}
		}
		float bonusHealing = Lua.LuaDoString<float>("return GetSpellBonusHealing();", "");
		float num5 = 0f;
		WoWUnit val6 = null;
		string text = "";
		uint num6 = 0u;
		bool flag3 = BuffManager.HaveBuff(((WoWObject)ObjectManager.Me).GetBaseAddress, 53390u) || BuffManager.HaveBuff(((WoWObject)ObjectManager.Me).GetBaseAddress, 51564u);
		bool flag4 = c.Resto.LowManaLhwEnable && ((WoWUnit)ObjectManager.Me).ManaPercentage < 30;
		foreach (WoWUnit p in list)
		{
			if (c.Resto.UseHealingWave && ResolveSpell("healing_wave") != 0 && !MovementManager.InMovement && !flag4)
			{
				float num7 = CalcHealScore(p, tank, GetExpectedHeal(ResolveSpell("healing_wave"), bonusHealing), c, c.Resto.AllowedOverhealPct, 2f, GetTankBonus(p, tank));
				if (flag3)
				{
					num7 *= 1.3f;
				}
				if (num7 > num5)
				{
					num5 = num7;
					val6 = p;
					text = "healing_wave";
					num6 = ResolveSpell("healing_wave");
				}
			}
			if (c.Resto.LesserHealingWaveEnable && ResolveSpell("lesser_healing_wave") != 0 && !MovementManager.InMovement)
			{
				float num7 = CalcHealScore(p, tank, GetExpectedHeal(ResolveSpell("lesser_healing_wave"), bonusHealing), c, c.Resto.AllowedOverhealPct, 1.5f, GetTankBonus(p, tank));
				if (flag3)
				{
					num7 *= 1.4f;
				}
				if (flag4)
				{
					num7 *= 2f;
				}
				if (num7 > num5)
				{
					num5 = num7;
					val6 = p;
					text = "lesser_healing_wave";
					num6 = ResolveSpell("lesser_healing_wave");
				}
			}
			if (c.Resto.UseRiptide && ResolveSpell("riptide") != 0 && SpellManager.GetSpellCooldownTimeLeft(ResolveSpell("riptide")) <= 0 && !BuffManager.HaveBuff(((WoWObject)p).GetBaseAddress, ResolveSpell("riptide")))
			{
				float num7 = CalcHealScore(p, tank, GetExpectedHeal(ResolveSpell("riptide"), bonusHealing), c, c.Resto.AllowedOverhealPct, 0f, c.Resto.RiptideTank ? GetTankBonus(p, tank, 1.5f) : 0f);
				if (MovementManager.InMovement)
				{
					num7 *= 3f;
				}
				if (num7 > num5)
				{
					num5 = num7;
					val6 = p;
					text = "riptide";
					num6 = ResolveSpell("riptide");
				}
			}
			if (!c.Resto.UseChainHeal || ResolveSpell("chain_heal") == 0 || MovementManager.InMovement || flag4)
			{
				continue;
			}
			float num8 = 0f;
			float expectedHeal = GetExpectedHeal(ResolveSpell("chain_heal"), bonusHealing);
			WoWUnit curr = p;
			List<WoWUnit> list3 = new List<WoWUnit>();
			List<WoWUnit> list4 = list.Where((WoWUnit t) => ((WoWObject)t).Guid != ((WoWObject)p).Guid).ToList();
			num8 += CalcHealScore(curr, tank, expectedHeal * (BuffManager.HaveBuff(((WoWObject)curr).GetBaseAddress, ResolveSpell("riptide")) ? 1.25f : 1f), c, c.Resto.AllowedOverhealPct, 2.5f);
			float nextHeal = expectedHeal * 0.6f;
			for (int num9 = 0; num9 < 3; num9++)
			{
				WoWUnit val7 = (from t in list4
					where ((WoWObject)curr).Position.DistanceTo2D(((WoWObject)t).Position) <= 12.5f
					orderby CalcHealScore(t, tank, nextHeal, c, c.Resto.AllowedOverhealPct, 2.5f, GetTankBonus(t, tank, 0.5f)) descending
					select t).FirstOrDefault();
				if (val7 == null)
				{
					break;
				}
				list3.Add(val7);
				list4.Remove(val7);
				num8 += CalcHealScore(val7, tank, nextHeal, c, c.Resto.AllowedOverhealPct, 2.5f);
				nextHeal *= 0.6f;
				curr = val7;
			}
			if (list3.Count >= 1 && num8 > num5)
			{
				num5 = num8;
				val6 = p;
				text = "chain_heal";
				num6 = ResolveSpell("chain_heal");
			}
		}
		if (val6 != null && num5 > 0f)
		{
			if (((WoWUnit)ObjectManager.Me).Target != ((WoWObject)val6).Guid)
			{
				SafeSetTarget(val6);
				return true;
			}
			SpellManager.CastSpellByIdLUA(num6);
			if (text == "riptide")
			{
				AddExpectedState("Riptide" + ((WoWObject)val6).Guid, 1000);
			}
			return true;
		}
		return false;
	}
}

// Clean Cache Buster
