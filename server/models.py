# models.py

class Player:
    def __init__(self, player_id, username, gold=0, score=0, kill_count=0):
        self.player_id = player_id
        self.username = username
        self.gold = gold
        self.score = score
        self.kill_count = kill_count

class Monster:
    def __init__(self, monster_id, name, hp, speed, reward_gold, score,
                 x=0, y=0, path_index=0, is_alive=True):
        self.monster_id = monster_id
        self.name = name
        self.hp = hp
        self.max_hp = hp
        self.speed = speed
        self.reward_gold = reward_gold
        self.score = score
        self.x = x
        self.y = y
        self.path_index = path_index
        self.is_alive = is_alive
        self.last_hit_player = None
        self.is_rewarded = False
        self.instance_id = None  # 唯一实例ID，格式 monster_<game_id>_<序号>

class Tower:
    def __init__(self, tower_id, attack, tower_range, cooldown,
                 cost=0, refund_rate=0.0, x=0, y=0, cooldown_timer=0,
                 owner_player_id=None, grid_x=0, grid_y=0):
        self.tower_id = tower_id
        self.attack = attack
        self.tower_range = tower_range
        self.cooldown = cooldown
        self.cooldown_timer = cooldown_timer
        self.cost = cost
        self.refund_rate = refund_rate
        self.x = x
        self.y = y
        self.owner_player_id = owner_player_id
        self.grid_x = grid_x
        self.grid_y = grid_y
        self.instance_id = None  # 唯一实例ID，格式 tower_<game_id>_<序号>

class GameState:
    def __init__(self, path_points=None, base_hp=100):
        self.players = {}        # player_id -> Player
        self.monsters = []       # list of Monster
        self.towers = []         # list of Tower
        self.path_points = path_points or []
        self.time_elapsed = 0.0
        self.is_game_over = False
        self.base_hp = base_hp
        self.tiles = {}          # "grid_x,grid_y" -> tile info
        self.monster_counter = 0 # 用于生成 instance_id
        self.tower_counter = 0   # 用于生成 instance_id