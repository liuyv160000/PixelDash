
============================================================
# fresh_hand_scene
============================================================

### [ex_manager] @ /ex_manager
  _enabled = true
  total_time = 90
  start_tips = "<time_showe#16>"
  victory_scene_name = "victory_scene"
  victory_delay = 0.6
  player = "<Node:player>"
  background = "<background_controler#66>"
  collection_poster = "<Node:collection_poster>"
  all_tips_ctrl = "<TextTipData#20>"
  scene_enemy_manager_new = null
  scene_enemy_manager = "<scene_enemy_manager#80>"
  use_new_enemy_manager = false
  music_manager = "<music_audio_manager#149>"
  tips = "<cc.Label#213>"
  plat_ver = null
  move_ctrl = "<ex_move_ctrl#230>"
  camera_ctrl = "<camera_controller#231>"
  near_site_move = null

### [ex_move_ctrl] @ /ex_manager
  _enabled = true

### [announcer] @ /ex_manager/Canvas/Layers/UI_camera/HUD/kill_announcer
  _enabled = true
  num_0 = "<uuid:f745c8e4..>"
  num_1 = "<uuid:eeae991e..>"
  num_2 = "<uuid:d8599e02..>"
  num_3 = "<uuid:e9dc0250..>"
  num_4 = "<uuid:dc2dc28e..>"
  num_5 = "<uuid:2f36be6e..>"
  num_6 = "<uuid:62b05764..>"
  num_7 = "<uuid:34fb4c38..>"
  num_8 = "<uuid:b1e7fcf4..>"
  num_9 = "<uuid:f5207c7d..>"
  kill_art_font = "<uuid:e0a4eb70..>"
  vertical_spacing = -80
  anim_start_scale = 0.5
  anim_end_scale = 1.2
  anim_duration = 0.3
  display_duration = 1.5
  fade_out_duration = 0.5

### [background_announcer] @ /ex_manager/Canvas/Layers/UI_camera/HUD/sp_announcer
  _enabled = true
  background_prefab = "<uuid:862d1a54..>"
  anim_start_scale = 0.5
  anim_end_scale = 1.2
  anim_duration = 0.3
  display_duration = 1
  fade_out_duration = 0.5
  enable_auto_test = false
  auto_test_interval = 3
  skill_ball_name = "bullet_skill_ball"
  check_interval = 0.1

### [time_showe] @ /ex_manager/Canvas/Layers/UI_camera/HUD/timer
  _enabled = true

### [TextTipData] @ /ex_manager/Canvas/Layers/UI_camera/HUD/tips
  _enabled = true
  tipLabel = "<cc.Label#19>"
  tipNode = "<Node:tips>"
  textTips = ["<??TextT#21>", "<??TextT#22>", "<??TextT#23>", "<??TextT#24>", "<??TextT#25>", "<??TextT#26>", "<??TextT#27>", "<??TextT#28>", "<??TextT#29>", "<??TextT#30>", "<??TextT#31>", "<??TextT#32>", "<??TextT#33>", "<??TextT#34>", "<??TextT#35>", "<??TextT#36>"]
  fadeInTime = 0.3
  fadeOutTime = 0.3

### [hp_line] @ /ex_manager/Canvas/Layers/UI_camera/HUD/hp_line
  _enabled = true
  player_node = "<Node:player>"

### [flashing] @ /ex_manager/Canvas/Layers/UI_camera/HUD/sta_tip
  _enabled = true
  flash_speed = 0.3
  min_opacity = 50
  max_opacity = 255
  auto_start = true

### [VirtualButton] @ /ex_manager/Canvas/Layers/UI_camera/UILayer/CtrlButton/down
  _enabled = true
  actionType = "DOWN"

### [VirtualButton] @ /ex_manager/Canvas/Layers/UI_camera/UILayer/CtrlButton/up
  _enabled = true
  actionType = "UP"

### [setting_button] @ /ex_manager/Canvas/Layers/UI_camera/UILayer/CtrlButton/setting
  _enabled = true
  setting_panel = "<Node:setting_background>"

### [button_change_scene] @ /ex_manager/Canvas/Layers/UI_camera/UILayer/CtrlButton/setting_background/重启
  _enabled = true
  targetScene = "fresh_hand_scene"
  button = "<cc.Button#183>"

### [button_change_scene] @ /ex_manager/Canvas/Layers/UI_camera/UILayer/CtrlButton/setting_background/返回
  _enabled = true
  targetScene = "ex_choosing_scene"
  button = "<cc.Button#188>"

### [coin_maker] @ /ex_manager/Canvas/Layers/GameLayer/collection_poster
  _enabled = true
  coin_prefab = "<uuid:204000ec..>"
  time_for_spawn_coins = [23, 23.5]
  coin_y_offsets = [150, 10]

### [skill_orb_maker] @ /ex_manager/Canvas/Layers/GameLayer/collection_poster
  _enabled = true
  skill_orb_prefab = "<uuid:3e0bb25c..>"
  skill_orb_spawn_times = [72]
  skill_orb_y_offsets = [10]
  skill_orb_pool_size = 3
  skill_orb_flight_speed = 500
  announcer = "<background_announcer#12>"

### [box_supply_maker] @ /ex_manager/Canvas/Layers/GameLayer/collection_poster
  _enabled = true
  supply_prefab = "<uuid:cabfe951..>"
  time_for_post_supply = []
  supply_y_offsets = []

### [box_supply_maker] @ /ex_manager/Canvas/Layers/GameLayer/collection_poster
  _enabled = true
  supply_prefab = "<uuid:339231fb..>"
  time_for_post_supply = []
  supply_y_offsets = []

### [box_supply_maker] @ /ex_manager/Canvas/Layers/GameLayer/collection_poster
  _enabled = true
  supply_prefab = "<uuid:f14cdf37..>"
  time_for_post_supply = []
  supply_y_offsets = []

### [coin_maker] @ /ex_manager/Canvas/Layers/GameLayer/collection_poster
  _enabled = true
  coin_prefab = "<uuid:204000ec..>"
  time_for_spawn_coins = [3, 32, 52]
  coin_y_offsets = [10, -10, 10]

### [coin_maker] @ /ex_manager/Canvas/Layers/GameLayer/collection_poster
  _enabled = true
  coin_prefab = "<uuid:204000ec..>"
  time_for_spawn_coins = [11, 11.1, 11.2, 11.3, 11.4, 42.1, 42.2, 42.3, 42.4, 42.5, 47, 47.5]
  coin_y_offsets = [150, 150, 150, 150, 150, -150, -150, -150, -150, -150, -150, -10]

### [background_controler] @ /ex_manager/Canvas/Layers/GameLayer/background_controler
  _enabled = true
  speed = 50
  timer = null

### [follow_with] @ /ex_manager/Canvas/Layers/GameLayer/background_controler
  _enabled = false
  target = "<Node:player>"
  smoothSpeed = 9
  followX = true
  followY = false
  followZ = true

### [back_clip_mover] @ /ex_manager/Canvas/Layers/GameLayer/background_controler/city_1
  _enabled = true

### [back_clip_mover] @ /ex_manager/Canvas/Layers/GameLayer/background_controler/city_2
  _enabled = true

### [back_clip_mover] @ /ex_manager/Canvas/Layers/GameLayer/background_controler/city_3
  _enabled = true

### [near_site_move] @ /ex_manager/Canvas/Layers/GameLayer/near_site
  _enabled = true
  move_speed = 8
  move_direction = -1

### [PlayerController] @ /ex_manager/Canvas/Layers/GameLayer/player
  _enabled = true
  on_line = false
  audioManager = "<player_audio_manager#98>"
  movementCtrl = "<player_movement_ctrl#200>"
  player_state = "idle"
  animation = "<cc.Animation#201>"
  transform = "<cc.UITransform#93>"
  skillSystem = "<SkillSystem#202>"

### [SkillSystem] @ /ex_manager/Canvas/Layers/GameLayer/player
  _enabled = true

### [BulletAutoSkill] @ /ex_manager/Canvas/Layers/GameLayer/player
  _enabled = true
  skillId = ""
  fireInterval = 0.5
  fireSound = "<cc.AudioSource#146>"

### [DaoSkill] @ /ex_manager/Canvas/Layers/GameLayer/player
  _enabled = true
  skillId = ""

### [TripleBulletAutoSkill] @ /ex_manager/Canvas/Layers/GameLayer/player
  _enabled = true
  skillId = ""
  fireInterval = 0.5
  fireSound = "<cc.AudioSource#146>"

### [BulletAutoSkill] @ /ex_manager/Canvas/Layers/GameLayer/player
  _enabled = true
  skillId = ""
  fireInterval = 0.5
  fireSound = "<cc.AudioSource#146>"

### [UnifieldInputManager] @ /ex_manager/Canvas/Layers/GameLayer/player
  _enabled = true

### [player_movement_ctrl] @ /ex_manager/Canvas/Layers/GameLayer/player
  _enabled = true
  rigidBody = "<cc.RigidBody2D#95>"
  collider = "<cc.CircleCollider2D#96>"
  x_speed = 0
  y_speed = 0
  Gravity = 0
  jump_force = 30
  quick_down_force = 90
  gravity_reverse = 1

### [player_audio_manager] @ /ex_manager/Canvas/Layers/GameLayer/player
  _enabled = true
  volume = 0.1
  jump_source = "<cc.AudioSource#99>"
  land_source = "<cc.AudioSource#143>"
  fire_source = "<cc.AudioSource#146>"

### [??8c497] @ /ex_manager/Canvas/Layers/GameLayer/player
  _enabled = true
  pool_init_size = 20

### [scene_enemy_manager] @ /ex_manager/Canvas/Layers/GameLayer/enemy_poster
  _enabled = true
  time_for_spqwn_sets = [76]
  pos_offset_sets = [50]
  patrol_dir_sets = [1]
  set_normal_fly_speed = 300
  set_normal_fly_damage = 10
  set_normal_fly_bullet_speed = 600
  set_normal_fly_bullet_damage = 5
  set_normal_fly_hp = 1
  time_for_spawn_circle_saw_sets = []
  set_circle_saw_speed = 600
  set_circle_saw_damage = 20
  time_for_spawn_laser_sets = [58, 58]
  spawn_laser_pos_offset_sets = [80, 130]
  set_laser_damage = 15
  set_laser_atk_time = 0.3
  set_warning_count = 1
  time_for_spawn_tracking_missile_sets = [5, 58, 60, 62, 64, 67, 67.1, 67.2, 67.3, 67.4, 67.5, 67.6, 67.7, 67.8, 67.9, 76, 77, 78]
  spawn_tracking_missile_pos_offset_sets = [50, 0, -50, 100, -100, 10, 20, 30, -40, -20, 10, 50, 100, 60, 77, 100, -50, -100]
  set_tracking_missile_speed = 900
  set_tracking_missile_max_speed = 1200
  set_tracking_missile_damage = 10
  set_tracking_missile_hp = 1
  normal_one_prefab = "<uuid:774f2cb2..>"
  circle_saw_prefab = "<uuid:00a9ddda..>"
  box_warning_zone_prefab = "<uuid:e69bd096..>"
  laser_prefab = "<uuid:7953c859..>"
  tracking_missile_prefab = "<uuid:d4e82f85..>"

### [near_site_move] @ /ex_manager/Canvas/Layers/GameLayer/plat_ver
  _enabled = true
  move_speed = 10
  move_direction = -1

### [plat_line] @ /ex_manager/Canvas/Layers/GameLayer/plat_ver/plat_line
  _enabled = true

### [camera_controller] @ /ex_manager/Canvas/Camera
  _enabled = true
  player = "<PlayerController#97>"
  player_movement_ctrl = "<player_movement_ctrl#200>"
  deadZoneCollider = "<cc.BoxCollider2D#222>"
  minFollowSpeed = 10
  followSpeedMultiplier = 4
  followX = true
  followY = true

### [music_audio_manager] @ /ex_manager/music_manager
  _enabled = true
  volume = 1
  audio_sources = ["<cc.AudioSource#151>"]
  audio_names = ["main_music_1"]

### [AutoSceneRecorder] @ /auto_scene_recorder
  _enabled = true

### [alter_audio_manager] @ /alter_audio_manager
  _enabled = true
  audio_managers = ["<player_audio_manager#142>", "<music_audio_manager#149>", "<tracking_missile_sound_effect_manager#106>", "<laser_sound_effect_manager#116>", "<normal_one_fly_sound_effect_manager#112>", "<circle_saw_sound_effect_manager#120>", "<warning_zone_sound_effect_manager#124>", "<coin_sound_effect_manager#128>", "<bullet_skill_ball_sound_effect_manager#132>"]
  volumeSlider = "<cc.Slider#152>"
  initial_volume = 0.5

### [tracking_missile_sound_effect_manager] @ /alter_audio_manager/tracking_missile_sound_effect_manager
  _enabled = true
  volume = 0.5
  missile_boom_source = "<cc.AudioSource#105>"

### [player_audio_manager] @ /alter_audio_manager/player_audio_manager
  _enabled = true
  volume = 0.5
  jump_source = "<cc.AudioSource#99>"
  land_source = "<cc.AudioSource#143>"
  fire_source = "<cc.AudioSource#146>"

### [normal_one_fly_sound_effect_manager] @ /alter_audio_manager/normal_one_fly_sound_effect_manager
  _enabled = true
  volume = 0.5
  normal_one_fly_attack_source = "<cc.AudioSource#109>"
  normal_one_fly_dead_source = "<cc.AudioSource#111>"

### [laser_sound_effect_manager] @ /alter_audio_manager/laser_sound_effect_manager
  _enabled = true
  volume = 0.5
  laser_attack_source = "<cc.AudioSource#115>"

### [circle_saw_sound_effect_manager] @ /alter_audio_manager/circle_saw_sound_effect_manager
  _enabled = true
  volume = 0.5
  circle_saw_hit_source = "<cc.AudioSource#119>"

### [warning_zone_sound_effect_manager] @ /alter_audio_manager/warning_zone_sound_effect_manager
  _enabled = true
  volume = 0.5
  warning_zone_enter_source = "<cc.AudioSource#123>"

### [coin_sound_effect_manager] @ /alter_audio_manager/coin_sound_effect_manager
  _enabled = true
  volume = 1
  coin_collect_source = "<cc.AudioSource#127>"

### [bullet_skill_ball_sound_effect_manager] @ /alter_audio_manager/bullet_skill_ball_sound_effect_manager
  _enabled = true
  volume = 1
  bullet_powerup_collect_source = "<cc.AudioSource#131>"

### [bullet_hit_player_sound_effect_manager] @ /alter_audio_manager/bullet_hit_player_sound_effect_manager
  _enabled = true
  volume = 1
  bullet_hit_player_source = "<cc.AudioSource#135>"

### [bullet_hit_enemy_sound_effect_manager] @ /alter_audio_manager/bullet_hit_enemy_sound_effect_manager
  _enabled = true
  volume = 1
  bullet_hit_enemy_source = "<cc.AudioSource#139>"

============================================================
# ex_scene_1
============================================================

### [ex_manager] @ /ex_manager
  _enabled = true
  total_time = 90
  start_tips = "<time_showe#16>"
  victory_scene_name = "victory_scene"
  victory_delay = 0.6
  player = "<Node:player>"
  background = "<background_controler#51>"
  collection_poster = "<Node:collection_poster>"
  all_tips_ctrl = "<TextTipData#20>"
  scene_enemy_manager_new = null
  scene_enemy_manager = "<scene_enemy_manager#65>"
  use_new_enemy_manager = false
  music_manager = "<music_audio_manager#134>"
  tips = "<cc.Label#197>"
  plat_ver = null
  move_ctrl = "<ex_move_ctrl#214>"
  camera_ctrl = "<camera_controller#215>"

### [ex_move_ctrl] @ /ex_manager
  _enabled = true

### [announcer] @ /ex_manager/Canvas/Layers/UI_camera/HUD/kill_announcer
  _enabled = true
  num_0 = "<uuid:f745c8e4..>"
  num_1 = "<uuid:eeae991e..>"
  num_2 = "<uuid:d8599e02..>"
  num_3 = "<uuid:e9dc0250..>"
  num_4 = "<uuid:dc2dc28e..>"
  num_5 = "<uuid:2f36be6e..>"
  num_6 = "<uuid:62b05764..>"
  num_7 = "<uuid:34fb4c38..>"
  num_8 = "<uuid:b1e7fcf4..>"
  num_9 = "<uuid:f5207c7d..>"
  kill_art_font = "<uuid:e0a4eb70..>"
  vertical_spacing = -80
  anim_start_scale = 0.5
  anim_end_scale = 1.2
  anim_duration = 0.3
  display_duration = 1.5
  fade_out_duration = 0.5

### [background_announcer] @ /ex_manager/Canvas/Layers/UI_camera/HUD/sp_announcer
  _enabled = true
  background_prefab = "<uuid:750050ac..>"
  anim_start_scale = 0.5
  anim_end_scale = 1.2
  anim_duration = 0.3
  display_duration = 1
  fade_out_duration = 0.5
  enable_auto_test = false
  auto_test_interval = 3
  skill_ball_name = "bullet_skill_ball"
  check_interval = 0.1

### [time_showe] @ /ex_manager/Canvas/Layers/UI_camera/HUD/timer
  _enabled = true

### [TextTipData] @ /ex_manager/Canvas/Layers/UI_camera/HUD/tips
  _enabled = true
  tipLabel = "<cc.Label#19>"
  tipNode = "<Node:tips>"
  textTips = []
  fadeInTime = 0.3
  fadeOutTime = 0.3

### [hp_line] @ /ex_manager/Canvas/Layers/UI_camera/HUD/hp_line
  _enabled = true
  player_node = "<Node:player>"

### [flashing] @ /ex_manager/Canvas/Layers/UI_camera/HUD/sta_tip
  _enabled = true
  flash_speed = 0.3
  min_opacity = 50
  max_opacity = 255
  auto_start = true

### [VirtualButton] @ /ex_manager/Canvas/Layers/UI_camera/UILayer/CtrlButton/down
  _enabled = true
  actionType = "DOWN"

### [VirtualButton] @ /ex_manager/Canvas/Layers/UI_camera/UILayer/CtrlButton/up
  _enabled = true
  actionType = "UP"

### [setting_button] @ /ex_manager/Canvas/Layers/UI_camera/UILayer/CtrlButton/setting
  _enabled = true
  setting_panel = "<Node:setting_background>"

### [button_change_scene] @ /ex_manager/Canvas/Layers/UI_camera/UILayer/CtrlButton/setting_background/重启
  _enabled = true
  targetScene = "ex_scene_1"
  button = "<cc.Button#168>"

### [button_change_scene] @ /ex_manager/Canvas/Layers/UI_camera/UILayer/CtrlButton/setting_background/返回
  _enabled = true
  targetScene = "ex_choosing_scene"
  button = "<cc.Button#173>"

### [coin_maker] @ /ex_manager/Canvas/Layers/GameLayer/collection_poster
  _enabled = true
  coin_prefab = "<uuid:204000ec..>"
  time_for_spawn_coins = [1.5, 1.7, 1.9, 2.1, 7, 7.1, 7.2, 7.3, 7.4]
  coin_y_offsets = [5, 5, 5, 5, -20, -20, -20, -20, -20]

### [skill_orb_maker] @ /ex_manager/Canvas/Layers/GameLayer/collection_poster
  _enabled = true
  skill_orb_prefab = "<uuid:3e0bb25c..>"
  skill_orb_spawn_times = [0]
  skill_orb_y_offsets = [10]
  skill_orb_pool_size = 3
  skill_orb_flight_speed = 500
  announcer = "<background_announcer#12>"

### [box_supply_maker] @ /ex_manager/Canvas/Layers/GameLayer/collection_poster
  _enabled = true
  supply_prefab = "<uuid:cabfe951..>"
  time_for_post_supply = [10]
  supply_y_offsets = [50]

### [box_supply_maker] @ /ex_manager/Canvas/Layers/GameLayer/collection_poster
  _enabled = true
  supply_prefab = "<uuid:339231fb..>"
  time_for_post_supply = [20, 25, 55]
  supply_y_offsets = [40, -40, 100]

### [box_supply_maker] @ /ex_manager/Canvas/Layers/GameLayer/collection_poster
  _enabled = true
  supply_prefab = "<uuid:f14cdf37..>"
  time_for_post_supply = [35, 45, 55]
  supply_y_offsets = [0, -10, 10]

### [skill_orb_maker] @ /ex_manager/Canvas/Layers/GameLayer/collection_poster
  _enabled = true
  skill_orb_prefab = "<uuid:3e0bb25c..>"
  skill_orb_spawn_times = [5]
  skill_orb_y_offsets = [20]
  skill_orb_pool_size = 3
  skill_orb_flight_speed = 500
  announcer = "<background_announcer#12>"

### [coin_maker] @ /ex_manager/Canvas/Layers/GameLayer/collection_poster
  _enabled = true
  coin_prefab = "<uuid:204000ec..>"
  time_for_spawn_coins = [14.3, 14.4, 14.5, 14.6, 14.7, 14.8, 14.9, 15, 15.1, 15.2, 15.3]
  coin_y_offsets = [-10, -60, -100, -150, -170, -184.9, -200, -180, -170, -150, -100]

### [coin_maker] @ /ex_manager/Canvas/Layers/GameLayer/collection_poster
  _enabled = true
  coin_prefab = "<uuid:204000ec..>"
  time_for_spawn_coins = [0, 0.1, 0.5, 1]
  coin_y_offsets = [0, 10, -50, 20]

### [background_controler] @ /ex_manager/Canvas/Layers/GameLayer/background_controler
  _enabled = true
  speed = 50
  timer = null

### [follow_with] @ /ex_manager/Canvas/Layers/GameLayer/background_controler
  _enabled = false
  target = "<Node:player>"
  smoothSpeed = 9
  followX = true
  followY = false
  followZ = true

### [back_clip_mover] @ /ex_manager/Canvas/Layers/GameLayer/background_controler/city_1
  _enabled = true

### [back_clip_mover] @ /ex_manager/Canvas/Layers/GameLayer/background_controler/city_2
  _enabled = true

### [back_clip_mover] @ /ex_manager/Canvas/Layers/GameLayer/background_controler/city_3
  _enabled = true

### [near_site_move] @ /ex_manager/Canvas/Layers/GameLayer/near_site
  _enabled = true
  move_speed = 8
  move_direction = -1

### [PlayerController] @ /ex_manager/Canvas/Layers/GameLayer/player
  _enabled = true
  on_line = false
  audioManager = "<player_audio_manager#83>"
  movementCtrl = "<player_movement_ctrl#185>"
  player_state = "idle"
  animation = "<cc.Animation#186>"
  transform = "<cc.UITransform#78>"
  skillSystem = "<SkillSystem#187>"

### [SkillSystem] @ /ex_manager/Canvas/Layers/GameLayer/player
  _enabled = true

### [BulletAutoSkill] @ /ex_manager/Canvas/Layers/GameLayer/player
  _enabled = true
  skillId = ""
  bulletPrefab = "<uuid:1a071a78..>"
  fireInterval = 0.5
  fireSound = "<cc.AudioSource#131>"

### [DaoSkill] @ /ex_manager/Canvas/Layers/GameLayer/player
  _enabled = true
  skillId = ""

### [TripleBulletAutoSkill] @ /ex_manager/Canvas/Layers/GameLayer/player
  _enabled = true
  skillId = ""
  bulletPrefab = "<uuid:1083a399..>"
  fireInterval = 0.5
  fireSound = "<cc.AudioSource#131>"

### [BulletAutoSkill] @ /ex_manager/Canvas/Layers/GameLayer/player
  _enabled = true
  skillId = ""
  bulletPrefab = "<uuid:1083a399..>"
  fireInterval = 0.5
  fireSound = "<cc.AudioSource#131>"

### [UnifieldInputManager] @ /ex_manager/Canvas/Layers/GameLayer/player
  _enabled = true

### [player_movement_ctrl] @ /ex_manager/Canvas/Layers/GameLayer/player
  _enabled = true
  rigidBody = "<cc.RigidBody2D#80>"
  collider = "<cc.CircleCollider2D#81>"
  x_speed = 0
  y_speed = 0
  Gravity = 0
  jump_force = 30
  quick_down_force = 90
  gravity_reverse = 1

### [player_audio_manager] @ /ex_manager/Canvas/Layers/GameLayer/player
  _enabled = true
  volume = 0.1
  jump_source = "<cc.AudioSource#84>"
  land_source = "<cc.AudioSource#128>"
  fire_source = "<cc.AudioSource#131>"

### [scene_enemy_manager] @ /ex_manager/Canvas/Layers/GameLayer/enemy_poster
  _enabled = true
  time_for_spqwn_sets = [10, 23, 23, 35, 40, 45, 57, 57, 57.5, 58, 58, 58.1]
  pos_offset_sets = [0, 10, -10, 30, -30, 0, 10, 10, -10, 300, 0, -300]
  patrol_dir_sets = [1, 1, -1, 1, -1, 1, 1, -1, 1, -1, 1, 1]
  set_normal_fly_speed = 400
  set_normal_fly_damage = 20
  set_normal_fly_bullet_speed = 1400
  set_normal_fly_bullet_damage = 10
  set_normal_fly_hp = 50
  time_for_spawn_circle_saw_sets = [5, 10, 27, 30, 39, 44, 48, 50, 55, 57, 59, 61, 63]
  set_circle_saw_speed = 1000
  set_circle_saw_damage = 30
  time_for_spawn_laser_sets = [7, 15, 29, 29, 35, 40, 45, 50, 55, 61, 63]
  spawn_laser_pos_offset_sets = [0, 0, 60, 120, 100, -100, 80, -80, 50, 50, -50]
  set_laser_damage = 30
  set_laser_atk_time = 0.5
  set_warning_count = 1
  time_for_spawn_tracking_missile_sets = [8, 9, 15, 16, 17, 18, 19, 27, 27, 27, 27, 40, 40.5, 41, 41.5, 42, 42.5, 43, 43.5, 44, 45, 46, 46.5, 47, 47.5, 48, 48.5, 50, 50.2, 50.4, 50.6, 50.8, 51, 51.2, 51.4, 51.6, 51.8, 52, 57, 58, 59, 60, 61, 62, 63, 64]
  spawn_tracking_missile_pos_offset_sets = [150, -150, 50, 20, -10, -30, -50, 80, 20, -20, -80, -10, -10, -10, -10, -10, -10, -10, -10, -10, -10, 50, -50, 30, -30, 10, -10, 300, 270, 240, 210, 180, 150, 180, 210, 240, 270, 300, 150, -170, 0, 50, 200, -80, -130, 10]
  set_tracking_missile_speed = 900
  set_tracking_missile_max_speed = 4800
  set_tracking_missile_damage = 20
  set_tracking_missile_hp = 40
  normal_one_prefab = "<uuid:774f2cb2..>"
  circle_saw_prefab = "<uuid:00a9ddda..>"
  box_warning_zone_prefab = "<uuid:e69bd096..>"
  laser_prefab = "<uuid:7953c859..>"
  tracking_missile_prefab = "<uuid:d4e82f85..>"

### [near_site_move] @ /ex_manager/Canvas/Layers/GameLayer/plat_ver
  _enabled = true
  move_speed = 10
  move_direction = -1

### [plat_line] @ /ex_manager/Canvas/Layers/GameLayer/plat_ver/plat_line
  _enabled = true

### [camera_controller] @ /ex_manager/Canvas/Camera
  _enabled = true
  player = "<PlayerController#82>"
  player_movement_ctrl = "<player_movement_ctrl#185>"
  deadZoneCollider = "<cc.BoxCollider2D#206>"
  minFollowSpeed = 10
  followSpeedMultiplier = 4
  followX = true
  followY = true

### [music_audio_manager] @ /ex_manager/music_manager
  _enabled = true
  volume = 1
  audio_sources = ["<cc.AudioSource#136>"]
  audio_names = ["main_music_1"]

### [AutoSceneRecorder] @ /auto_scene_recorder
  _enabled = true

### [alter_audio_manager] @ /alter_audio_manager
  _enabled = true
  audio_managers = ["<player_audio_manager#127>", "<music_audio_manager#134>", "<tracking_missile_sound_effect_manager#91>", "<laser_sound_effect_manager#101>", "<normal_one_fly_sound_effect_manager#97>", "<circle_saw_sound_effect_manager#105>", "<warning_zone_sound_effect_manager#109>", "<coin_sound_effect_manager#113>", "<bullet_skill_ball_sound_effect_manager#117>"]
  volumeSlider = "<cc.Slider#137>"
  initial_volume = 0.5

### [tracking_missile_sound_effect_manager] @ /alter_audio_manager/tracking_missile_sound_effect_manager
  _enabled = true
  volume = 0.5
  missile_boom_source = "<cc.AudioSource#90>"

### [player_audio_manager] @ /alter_audio_manager/player_audio_manager
  _enabled = true
  volume = 0.5
  jump_source = "<cc.AudioSource#84>"
  land_source = "<cc.AudioSource#128>"
  fire_source = "<cc.AudioSource#131>"

### [normal_one_fly_sound_effect_manager] @ /alter_audio_manager/normal_one_fly_sound_effect_manager
  _enabled = true
  volume = 0.5
  normal_one_fly_attack_source = "<cc.AudioSource#94>"
  normal_one_fly_dead_source = "<cc.AudioSource#96>"

### [laser_sound_effect_manager] @ /alter_audio_manager/laser_sound_effect_manager
  _enabled = true
  volume = 0.5
  laser_attack_source = "<cc.AudioSource#100>"

### [circle_saw_sound_effect_manager] @ /alter_audio_manager/circle_saw_sound_effect_manager
  _enabled = true
  volume = 0.5
  circle_saw_hit_source = "<cc.AudioSource#104>"

### [warning_zone_sound_effect_manager] @ /alter_audio_manager/warning_zone_sound_effect_manager
  _enabled = true
  volume = 0.5
  warning_zone_enter_source = "<cc.AudioSource#108>"

### [coin_sound_effect_manager] @ /alter_audio_manager/coin_sound_effect_manager
  _enabled = true
  volume = 1
  coin_collect_source = "<cc.AudioSource#112>"

### [bullet_skill_ball_sound_effect_manager] @ /alter_audio_manager/bullet_skill_ball_sound_effect_manager
  _enabled = true
  volume = 1
  bullet_powerup_collect_source = "<cc.AudioSource#116>"

### [bullet_hit_player_sound_effect_manager] @ /alter_audio_manager/bullet_hit_player_sound_effect_manager
  _enabled = true
  volume = 1
  bullet_hit_player_source = "<cc.AudioSource#120>"

### [bullet_hit_enemy_sound_effect_manager] @ /alter_audio_manager/bullet_hit_enemy_sound_effect_manager
  _enabled = true
  volume = 1
  bullet_hit_enemy_source = "<cc.AudioSource#124>"