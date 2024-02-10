﻿namespace L2Dn.GameServer.Network;

public enum ServerPacketCode: byte
{
	DIE = 0x00,
	REVIVE = 0x01,
	ATTACK_OUT_OF_RANGE = 0x02,
	ATTACKIN_COOL_TIME = 0x03,
	ATTACK_DEAD_TARGET = 0x04,
	SPAWN_ITEM = 0x05,
	SELL_LIST = 0x06,
	BUY_LIST = 0x07,
	DELETE_OBJECT = 0x08,
	CHARACTER_SELECTION_INFO = 0x09,
	LOGIN_FAIL = 0x0A,
	CHARACTER_SELECTED = 0x0B,
	NPC_INFO = 0x0C,
	NEW_CHARACTER_SUCCESS = 0x0D,
	NEW_CHARACTER_FAIL = 0x0E,
	CHARACTER_CREATE_SUCCESS = 0x0F,
	CHARACTER_CREATE_FAIL = 0x10,
	ITEM_LIST = 0x11,
	SUN_RISE = 0x12,
	SUN_SET = 0x13,
	TRADE_START = 0x14,
	TRADE_START_OK = 0x15,
	DROP_ITEM = 0x16,
	GET_ITEM = 0x17,
	STATUS_UPDATE = 0x18,
	NPC_HTML_MESSAGE = 0x19,
	TRADE_OWN_ADD = 0x1A,
	TRADE_OTHER_ADD = 0x1B,
	TRADE_DONE = 0x1C,
	CHARACTER_DELETE_SUCCESS = 0x1D,
	CHARACTER_DELETE_FAIL = 0x1E,
	ACTION_FAIL = 0x1F,
	SEVER_CLOSE = 0x20,
	INVENTORY_UPDATE = 0x21,
	TELEPORT_TO_LOCATION = 0x22,
	TARGET_SELECTED = 0x23,
	TARGET_UNSELECTED = 0x24,
	AUTO_ATTACK_START = 0x25,
	AUTO_ATTACK_STOP = 0x26,
	SOCIAL_ACTION = 0x27,
	CHANGE_MOVE_TYPE = 0x28,
	CHANGE_WAIT_TYPE = 0x29,
	MANAGE_PLEDGE_POWER = 0x2A,
	CREATE_PLEDGE = 0x2B,
	ASK_JOIN_PLEDGE = 0x2C,
	JOIN_PLEDGE = 0x2D,
	VERSION_CHECK = 0x2E,
	MOVE_TO_LOCATION = 0x2F,
	NPC_SAY = 0x30,
	CHAR_INFO = 0x31,
	USER_INFO = 0x32,
	ATTACK = 0x33,
	WITHDRAWAL_PLEDGE = 0x34,
	OUST_PLEDGE_MEMBER = 0x35,
	SET_OUST_PLEDGE_MEMBER = 0x36,
	DISMISS_PLEDGE = 0x37,
	SET_DISMISS_PLEDGE = 0x38,
	ASK_JOIN_PARTY = 0x39,
	JOIN_PARTY = 0x3A,
	WITHDRAWAL_PARTY = 0x3B,
	OUST_PARTY_MEMBER = 0x3C,
	SET_OUST_PARTY_MEMBER = 0x3D,
	DISMISS_PARTY = 0x3E,
	SET_DISMISS_PARTY = 0x3F,
	MAGIC_AND_SKILL_LIST = 0x40,
	WAREHOUSE_DEPOSIT_LIST = 0x41,
	WAREHOUSE_WITHDRAW_LIST = 0x42,
	WAREHOUSE_DONE = 0x43,
	SHORT_CUT_REGISTER = 0x44,
	SHORT_CUT_INIT = 0x45,
	SHORT_CUT_DELETE = 0x46,
	STOP_MOVE = 0x47,
	MAGIC_SKILL_USE = 0x48,
	MAGIC_SKILL_CANCELED = 0x49,
	SAY2 = 0x4A,
	NPC_INFO_ABNORMAL_VISUAL_EFFECT = 0x4B,
	DOOR_INFO = 0x4C,
	DOOR_STATUS_UPDATE = 0x4D,
	PARTY_SMALL_WINDOW_ALL = 0x4E,
	PARTY_SMALL_WINDOW_ADD = 0x4F,
	PARTY_SMALL_WINDOW_DELETE_ALL = 0x50,
	PARTY_SMALL_WINDOW_DELETE = 0x51,
	PARTY_SMALL_WINDOW_UPDATE = 0x52,
	TRADE_PRESS_OWN_OK = 0x53,
	MAGIC_SKILL_LAUNCHED = 0x54,
	FRIEND_ADD_REQUEST_RESULT = 0x55,
	FRIEND_ADD = 0x56,
	FRIEND_REMOVE = 0x57,
	FRIEND_LIST = 0x58,
	FRIEND_STATUS = 0x59,
	PLEDGE_SHOW_MEMBER_LIST_ALL = 0x5A,
	PLEDGE_SHOW_MEMBER_LIST_UPDATE = 0x5B,
	PLEDGE_SHOW_MEMBER_LIST_ADD = 0x5C,
	PLEDGE_SHOW_MEMBER_LIST_DELETE = 0x5D,
	MAGIC_LIST = 0x5E,
	SKILL_LIST = 0x5F,
	VEHICLE_INFO = 0x60,
	FINISH_ROTATING = 0x61,
	SYSTEM_MESSAGE = 0x62,
	START_PLEDGE_WAR = 0x63,
	REPLY_START_PLEDGE_WAR = 0x64,
	STOP_PLEDGE_WAR = 0x65,
	REPLY_STOP_PLEDGE_WAR = 0x66,
	SURRENDER_PLEDGE_WAR = 0x67,
	REPLY_SURRENDER_PLEDGE_WAR = 0x68,
	SET_PLEDGE_CREST = 0x69,
	PLEDGE_CREST = 0x6A,
	SETUP_GAUGE = 0x6B,
	VEHICLE_DEPARTURE = 0x6C,
	VEHICLE_CHECK_LOCATION = 0x6D,
	GET_ON_VEHICLE = 0x6E,
	GET_OFF_VEHICLE = 0x6F,
	TRADE_REQUEST = 0x70,
	RESTART_RESPONSE = 0x71,
	MOVE_TO_PAWN = 0x72,
	SSQ_INFO = 0x73,
	GAME_GUARD_QUERY = 0x74,
	L2_FRIEND_LIST = 0x75,
	L2_FRIEND = 0x76,
	L2_FRIEND_STATUS = 0x77,
	L2_FRIEND_SAY = 0x78,
	VALIDATE_LOCATION = 0x79,
	START_ROTATING = 0x7A,
	SHOW_BOARD = 0x7B,
	CHOOSE_INVENTORY_ITEM = 0x7C,
	DUMMY = 0x7D,
	MOVE_TO_LOCATION_IN_VEHICLE = 0x7E,
	STOP_MOVE_IN_VEHICLE = 0x7F,
	VALIDATE_LOCATION_IN_VEHICLE = 0x80,
	TRADE_UPDATE = 0x81,
	TRADE_PRESS_OTHER_OK = 0x82,
	FRIEND_ADD_REQUEST = 0x83,
	LOG_OUT_OK = 0x84,
	ABNORMAL_STATUS_UPDATE = 0x85,
	QUEST_LIST = 0x86,
	ENCHANT_RESULT = 0x87,
	PLEDGE_SHOW_MEMBER_LIST_DELETE_ALL = 0x88,
	PLEDGE_INFO = 0x89,
	PLEDGE_EXTENDED_INFO = 0x8A,
	SUMMON_INFO = 0x8B,
	RIDE = 0x8C,
	DUMMY2 = 0x8D,
	PLEDGE_SHOW_INFO_UPDATE = 0x8E,
	CLIENT_ACTION = 0x8F,
	ACQUIRE_SKILL_LIST = 0x90,
	ACQUIRE_SKILL_INFO = 0x91,
	SERVER_OBJECT_INFO = 0x92,
	GM_HIDE = 0x93,
	ACQUIRE_SKILL_DONE = 0x94,
	GM_VIEW_CHARACTER_INFO = 0x95,
	GM_VIEW_PLEDGE_INFO = 0x96,
	GM_VIEW_SKILL_INFO = 0x97,
	GM_VIEW_MAGIC_INFO = 0x98,
	GM_VIEW_QUEST_INFO = 0x99,
	GM_VIEW_ITEM_LIST = 0x9A,
	GM_VIEW_WAREHOUSE_WITHDRAW_LIST = 0x9B,
	LIST_PARTY_WATING = 0x9C,
	PARTY_ROOM_INFO = 0x9D,
	PLAY_SOUND = 0x9E,
	STATIC_OBJECT = 0x9F,
	PRIVATE_STORE_MANAGE_LIST = 0xA0,
	PRIVATE_STORE_LIST = 0xA1,
	PRIVATE_STORE_MSG = 0xA2,
	SHOW_MINIMAP = 0xA3,
	REVIVE_REQUEST = 0xA4,
	ABNORMAL_VISUAL_EFFECT = 0xA5,
	TUTORIAL_SHOW_HTML = 0xA6,
	TUTORIAL_SHOW_QUESTION_MARK = 0xA7,
	TUTORIAL_ENABLE_CLIENT_EVENT = 0xA8,
	TUTORIAL_CLOSE_HTML = 0xA9,
	SHOW_RADAR = 0xAA,
	WITHDRAW_ALLIANCE = 0xAB,
	OUST_ALLIANCE_MEMBER_PLEDGE = 0xAC,
	DISMISS_ALLIANCE = 0xAD,
	SET_ALLIANCE_CREST = 0xAE,
	ALLIANCE_CREST = 0xAF,
	SERVER_CLOSE_SOCKET = 0xB0,
	PET_STATUS_SHOW = 0xB1,
	PET_INFO = 0xB2,
	PET_ITEM_LIST = 0xB3,
	PET_INVENTORY_UPDATE = 0xB4,
	ALLIANCE_INFO = 0xB5,
	PET_STATUS_UPDATE = 0xB6,
	PET_DELETE = 0xB7,
	DELETE_RADAR = 0xB8,
	MY_TARGET_SELECTED = 0xB9,
	PARTY_MEMBER_POSITION = 0xBA,
	ASK_JOIN_ALLIANCE = 0xBB,
	JOIN_ALLIANCE = 0xBC,
	PRIVATE_STORE_BUY_MANAGE_LIST = 0xBD,
	PRIVATE_STORE_BUY_LIST = 0xBE,
	PRIVATE_STORE_BUY_MSG = 0xBF,
	VEHICLE_START = 0xC0,
	NPC_INFO_STATE = 0xC1,
	START_ALLIANCE_WAR = 0xC2,
	REPLY_START_ALLIANCE_WAR = 0xC3,
	STOP_ALLIANCE_WAR = 0xC4,
	REPLY_STOP_ALLIANCE_WAR = 0xC5,
	SURRENDER_ALLIANCE_WAR = 0xC6,
	SKILL_COOL_TIME = 0xC7,
	PACKAGE_TO_LIST = 0xC8,
	CASTLE_SIEGE_INFO = 0xC9,
	CASTLE_SIEGE_ATTACKER_LIST = 0xCA,
	CASTLE_SIEGE_DEFENDER_LIST = 0xCB,
	NICK_NAME_CHANGED = 0xCC,
	PLEDGE_STATUS_CHANGED = 0xCD,
	RELATION_CHANGED = 0xCE,
	EVENT_TRIGGER = 0xCF,
	MULTI_SELL_LIST = 0xD0,
	SET_SUMMON_REMAIN_TIME = 0xD1,
	PACKAGE_SENDABLE_LIST = 0xD2,
	EARTHQUAKE = 0xD3,
	FLY_TO_LOCATION = 0xD4,
	BLOCK_LIST = 0xD5,
	SPECIAL_CAMERA = 0xD6,
	NORMAL_CAMERA = 0xD7,
	SKILL_REMAIN_SEC = 0xD8,
	NET_PING = 0xD9,
	DICE = 0xDA,
	SNOOP = 0xDB,
	RECIPE_BOOK_ITEM_LIST = 0xDC,
	RECIPE_ITEM_MAKE_INFO = 0xDD,
	RECIPE_SHOP_MANAGE_LIST = 0xDE,
	RECIPE_SHOP_SELL_LIST = 0xDF,
	RECIPE_SHOP_ITEM_INFO = 0xE0,
	RECIPE_SHOP_MSG = 0xE1,
	SHOW_CALC = 0xE2,
	MON_RACE_INFO = 0xE3,
	HENNA_ITEM_INFO = 0xE4,
	HENNA_INFO = 0xE5,
	HENNA_UNEQUIP_LIST = 0xE6,
	HENNA_UNEQUIP_INFO = 0xE7,
	MACRO_LIST = 0xE8,
	BUY_LIST_SEED = 0xE9,
	SHOW_TOWN_MAP = 0xEA,
	OBSERVER_START = 0xEB,
	OBSERVER_END = 0xEC,
	CHAIR_SIT = 0xED,
	HENNA_EQUIP_LIST = 0xEE,
	SELL_LIST_PROCURE = 0xEF,
	GMHENNA_INFO = 0xF0,
	RADAR_CONTROL = 0xF1,
	CLIENT_SET_TIME = 0xF2,
	CONFIRM_DLG = 0xF3,
	PARTY_SPELLED = 0xF4,
	SHOP_PREVIEW_LIST = 0xF5,
	SHOP_PREVIEW_INFO = 0xF6,
	CAMERA_MODE = 0xF7,
	SHOW_XMAS_SEAL = 0xF8,
	ETC_STATUS_UPDATE = 0xF9,
	SHORT_BUFF_STATUS_UPDATE = 0xFA,
	SSQ_STATUS = 0xFB,
	PETITION_VOTE = 0xFC,
	AGIT_DECO_INFO = 0xFD,
	EXTENDED = 0xFE,
}