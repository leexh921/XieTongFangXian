# models.py


class Player:
    def __init__(self, player_id, username, gold=0, score=0, kill_count=0):
        self.player_id = player_id
        self.username = username
        self.gold = gold
        self.score = score
        self.kill_count = kill_count


class Monster:
    def __init__(self, monster_id, name, hp, speed, reward_gold, score_value,
                 damage_to_base=1, x=0.0, y=0.0, path_index=0, is_alive=True):
        self.monster_id = monster_id
        self.name = name
        self.hp = hp
        self.max_hp = hp
        self.speed = speed
        self.reward_gold = reward_gold
        self.score_value = score_value
        self.damage_to_base = damage_to_base
        self.x = float(x)
        self.y = float(y)
        self.path_index = path_index
        self.is_alive = is_alive
        self.is_rewarded = False
        self.instance_id = None
        self._dist_on_segment = 0.0

    def advance(self, path_points, tick_interval):
        """沿路径点插值移动，返回本次移动距上一个路径点的距离"""
        if self.path_index >= len(path_points) - 1:
            if not self.is_rewarded:
                self.is_rewarded = True
            return

        p0 = path_points[self.path_index]
        p1 = path_points[self.path_index + 1]
        dx = p1[0] - p0[0]
        dy = p1[1] - p0[1]
        seg_len = (dx * dx + dy * dy) ** 0.5

        if seg_len == 0:
            self.path_index += 1
            self._dist_on_segment = 0.0
            return

        self._dist_on_segment += self.speed * tick_interval

        while self._dist_on_segment >= seg_len and self.path_index < len(path_points) - 1:
            self._dist_on_segment -= seg_len
            self.path_index += 1
            if self.path_index >= len(path_points) - 1:
                self.x = float(path_points[-1][0])
                self.y = float(path_points[-1][1])
                return
            p0 = path_points[self.path_index]
            p1 = path_points[self.path_index + 1]
            dx = p1[0] - p0[0]
            dy = p1[1] - p0[1]
            seg_len = (dx * dx + dy * dy) ** 0.5

        t = self._dist_on_segment / seg_len if seg_len > 0 else 0
        self.x = p0[0] + dx * t
        self.y = p0[1] + dy * t


class Tower:
    def __init__(self, tower_id, attack, tower_range, cooldown,
                 cost=0, refund_rate=0.0, cooldown_timer=0.0,
                 owner_player_id=None, grid_x=0, grid_y=0):
        self.tower_id = tower_id
        self.attack = attack
        self.tower_range = tower_range
        self.cooldown = cooldown
        self.cooldown_timer = cooldown_timer
        self.cost = cost
        self.refund_rate = refund_rate
        self.owner_player_id = owner_player_id
        self.grid_x = grid_x
        self.grid_y = grid_y
        self.instance_id = None


class GameState:
    def __init__(self, path_points=None, base_hp=100):
        self.players = {}
        self.monsters = []
        self.towers = []
        self.path_points = path_points or []
        self.time_elapsed = 0.0
        self.is_game_over = False
        self.base_hp = base_hp
        self.tiles = {}
        self.monster_counter = 0
        self.tower_counter = 0
        self.gold_accumulator = 0.0
