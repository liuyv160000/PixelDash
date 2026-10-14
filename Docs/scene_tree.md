# 关卡场景节点树规格
生成方式：Python 解析 Cocos .scene JSON（compressUuid 前5hex → @ccclass）

## fresh_hand_scene.scene（243 对象）
ex_manager (0,0) layer=128
    └components: ex_manager, ex_move_ctrl
  Canvas (640,360) layer=128
      └components: cc.UITransform, cc.Canvas, cc.Widget
    Layers (0,0) layer=128
        └components: cc.UITransform
      UI_camera (0,0) layer=512
          └components: cc.Camera
        HUD (0,0) layer=512
            └components: cc.UITransform, cc.Widget
          kill_announcer (434,146) layer=512
              └components: cc.UITransform, announcer
          sp_announcer (0,0) layer=512
              └components: cc.UITransform, background_announcer
          timer (0,229) layer=512
              └components: cc.UITransform, cc.Label, time_showe
          tips (0,120) layer=512
              └components: cc.UITransform, cc.Label, TextTipData
          hp_line (-267,234) layer=512
              └components: cc.UITransform, cc.Sprite, hp_line, cc.Widget
          sta_tip (0,134) layer=512
              └components: cc.UITransform, cc.Label, flashing
        UILayer (0,0) layer=512
            └components: cc.UITransform, cc.Widget
          CtrlButton (0,0) layer=512
              └components: cc.UITransform
            down (-426,-229) layer=512
                └components: cc.UITransform, cc.Sprite, cc.Button, VirtualButton
            up (-423,-134) layer=512
                └components: cc.UITransform, cc.Sprite, VirtualButton, cc.Button
            setting (440,244) layer=512
                └components: cc.UITransform, cc.Sprite, cc.Button, setting_button, cc.Widget
            [x]setting_background (-8,-24) layer=512
                └components: cc.UITransform, cc.Sprite
              设置字体 (-11,186) layer=512
                  └components: cc.UITransform, cc.Sprite
              重启 (-28,-150) layer=512
                  └components: cc.UITransform, cc.Sprite, cc.Button, button_change_scene
              返回 (-240,-150) layer=512
                  └components: cc.UITransform, cc.Sprite, cc.Button, button_change_scene
              音量 (0,0) layer=512
                  └components: cc.UITransform, cc.Sprite, cc.Slider
                Handle (-120,0) layer=512
                    └components: cc.UITransform, cc.Sprite, cc.Button
      GameLayer (0,0) layer=128
          └components: cc.UITransform
        collection_poster (636,-36) layer=128
            └components: cc.UITransform, cc.Sprite, coin_maker, skill_orb_maker, box_supply_maker, box_supply_maker, box_supply_maker, coin_maker, coin_maker
        background_controler (-201,0) layer=128
            └components: background_controler, follow_with
          city_1 (-2436,18) layer=128
              └components: cc.UITransform, cc.Sprite, back_clip_mover
          city_2 (1406,18) layer=128
              └components: cc.UITransform, cc.Sprite, back_clip_mover
          city_3 (5268,18) layer=128
              └components: cc.UITransform, cc.Sprite, back_clip_mover
        near_site (0,0) layer=128
            └components: cc.UITransform, near_site_move
          大楼_1 (3340,543) layer=128
              └components: cc.UITransform, cc.Sprite
          广告牌_1 (7497,-157) layer=128
              └components: cc.UITransform, cc.Sprite
        player (-335,-19) layer=128
            └components: cc.UITransform, cc.Sprite, cc.RigidBody2D, cc.CircleCollider2D, PlayerController, cc.Animation, SkillSystem, BulletAutoSkill, DaoSkill, TripleBulletAutoSkill, BulletAutoSkill, UnifieldInputManager, player_movement_ctrl, player_audio_manager, cc.CircleCollider2D, ??8c497
          audios (0,0) layer=128
              └components: cc.UITransform
        enemy_poster (1266,-44) layer=128
            └components: cc.UITransform, cc.Sprite, scene_enemy_manager
        plat_ver (0,0) layer=128
            └components: cc.UITransform, near_site_move
          plat_line (-13,-37) layer=128
              └components: cc.UITransform, cc.Sprite, cc.RigidBody2D, cc.BoxCollider2D, plat_line
    Camera (0,16) layer=128
        └components: cc.Camera, camera_controller
      dead_zone (-351,11) layer=128
          └components: cc.UITransform, cc.BoxCollider2D, cc.Sprite
  music_manager (0,0) layer=128
      └components: cc.AudioSource, music_audio_manager
auto_scene_recorder (0,0) layer=1073741824
    └components: AutoSceneRecorder
alter_audio_manager (0,0) layer=1073741824
    └components: alter_audio_manager
  tracking_missile_sound_effect_manager (0,0) layer=1073741824
      └components: tracking_missile_sound_effect_manager
    missile_boom_sound (0,0) layer=1073741824
        └components: cc.AudioSource
  player_audio_manager (0,0) layer=1073741824
      └components: player_audio_manager
    fire_source (305,341) layer=128
        └components: cc.UITransform, cc.AudioSource
    land_source (305,341) layer=128
        └components: cc.UITransform, cc.AudioSource
    jump_source (305,341) layer=128
        └components: cc.UITransform, cc.AudioSource
  normal_one_fly_sound_effect_manager (0,0) layer=1073741824
      └components: normal_one_fly_sound_effect_manager
    激光枪射击_耳聆网_[声音ID：13323] (0,0) layer=1073741824
        └components: cc.AudioSource
    爆炸音效-像素破坏-怀旧游戏_爱给网_aigei_com (0,0) layer=1073741824
        └components: cc.AudioSource
  laser_sound_effect_manager (0,0) layer=1073741824
      └components: laser_sound_effect_manager
    激光的声音_耳聆网_[声音ID：10124] (0,0) layer=1073741824
        └components: cc.AudioSource
  circle_saw_sound_effect_manager (0,0) layer=1073741824
      └components: circle_saw_sound_effect_manager
    电锯切割到敌人 (0,0) layer=1073741824
        └components: cc.AudioSource
  warning_zone_sound_effect_manager (0,0) layer=1073741824
      └components: warning_zone_sound_effect_manager
    警示音效 (0,0) layer=1073741824
        └components: cc.AudioSource
  coin_sound_effect_manager (0,0) layer=1073741824
      └components: coin_sound_effect_manager
    拾取道具 (0,0) layer=1073741824
        └components: cc.AudioSource
  bullet_skill_ball_sound_effect_manager (0,0) layer=1073741824
      └components: bullet_skill_ball_sound_effect_manager
    补给箱放出补给的音效 (0,0) layer=1073741824
        └components: cc.AudioSource
  bullet_hit_player_sound_effect_manager (0,0) layer=1073741824
      └components: bullet_hit_player_sound_effect_manager
    角色被击中 (0,0) layer=1073741824
        └components: cc.AudioSource
  bullet_hit_enemy_sound_effect_manager (0,0) layer=1073741824
      └components: bullet_hit_enemy_sound_effect_manager
    玩家子弹命中 (0,0) layer=1073741824
        └components: cc.AudioSource


## ex_scene_1.scene（227 对象）
ex_manager (0,0) layer=128
    └components: ex_manager, ex_move_ctrl
  Canvas (640,360) layer=128
      └components: cc.UITransform, cc.Canvas, cc.Widget
    Layers (0,0) layer=128
        └components: cc.UITransform
      UI_camera (0,0) layer=512
          └components: cc.Camera
        HUD (0,0) layer=512
            └components: cc.UITransform, cc.Widget
          kill_announcer (434,146) layer=512
              └components: cc.UITransform, announcer
          sp_announcer (0,0) layer=512
              └components: cc.UITransform, background_announcer
          timer (0,229) layer=512
              └components: cc.UITransform, cc.Label, time_showe
          tips (0,120) layer=512
              └components: cc.UITransform, cc.Label, TextTipData
          hp_line (-267,234) layer=512
              └components: cc.UITransform, cc.Sprite, hp_line, cc.Widget
          sta_tip (0,134) layer=512
              └components: cc.UITransform, cc.Label, flashing
        UILayer (0,0) layer=512
            └components: cc.UITransform, cc.Widget
          CtrlButton (0,0) layer=512
              └components: cc.UITransform
            down (-426,-229) layer=512
                └components: cc.UITransform, cc.Sprite, cc.Button, VirtualButton
            up (-423,-134) layer=512
                └components: cc.UITransform, cc.Sprite, VirtualButton, cc.Button
            setting (440,244) layer=512
                └components: cc.UITransform, cc.Sprite, cc.Button, setting_button, cc.Widget
            [x]setting_background (-8,-24) layer=512
                └components: cc.UITransform, cc.Sprite
              设置字体 (-11,186) layer=512
                  └components: cc.UITransform, cc.Sprite
              重启 (-28,-150) layer=512
                  └components: cc.UITransform, cc.Sprite, cc.Button, button_change_scene
              返回 (-240,-150) layer=512
                  └components: cc.UITransform, cc.Sprite, cc.Button, button_change_scene
              音量 (0,0) layer=512
                  └components: cc.UITransform, cc.Sprite, cc.Slider
                Handle (-120,0) layer=512
                    └components: cc.UITransform, cc.Sprite, cc.Button
      GameLayer (0,0) layer=128
          └components: cc.UITransform
        collection_poster (587,-36) layer=128
            └components: cc.UITransform, cc.Sprite, coin_maker, skill_orb_maker, box_supply_maker, box_supply_maker, box_supply_maker, skill_orb_maker, coin_maker, coin_maker
        background_controler (-201,0) layer=128
            └components: background_controler, follow_with
          city_1 (-2436,18) layer=128
              └components: cc.UITransform, cc.Sprite, back_clip_mover
          city_2 (1406,18) layer=128
              └components: cc.UITransform, cc.Sprite, back_clip_mover
          city_3 (5268,18) layer=128
              └components: cc.UITransform, cc.Sprite, back_clip_mover
        near_site (0,0) layer=128
            └components: cc.UITransform, near_site_move
          大楼_1 (3340,543) layer=128
              └components: cc.UITransform, cc.Sprite
          广告牌_1 (7497,-157) layer=128
              └components: cc.UITransform, cc.Sprite
        player (-335,-19) layer=128
            └components: cc.UITransform, cc.Sprite, cc.RigidBody2D, cc.CircleCollider2D, PlayerController, cc.Animation, SkillSystem, BulletAutoSkill, DaoSkill, TripleBulletAutoSkill, BulletAutoSkill, UnifieldInputManager, player_movement_ctrl, player_audio_manager, cc.CircleCollider2D
          audios (0,0) layer=128
              └components: cc.UITransform
        enemy_poster (1266,-44) layer=128
            └components: cc.UITransform, cc.Sprite, scene_enemy_manager
        plat_ver (0,0) layer=128
            └components: cc.UITransform, near_site_move
          plat_line (-13,-37) layer=128
              └components: cc.UITransform, cc.Sprite, cc.RigidBody2D, cc.BoxCollider2D, plat_line
    Camera (0,16) layer=128
        └components: cc.Camera, camera_controller
      dead_zone (-351,11) layer=128
          └components: cc.UITransform, cc.BoxCollider2D, cc.Sprite
  music_manager (0,0) layer=128
      └components: cc.AudioSource, music_audio_manager
auto_scene_recorder (0,0) layer=1073741824
    └components: AutoSceneRecorder
alter_audio_manager (0,0) layer=1073741824
    └components: alter_audio_manager
  tracking_missile_sound_effect_manager (0,0) layer=1073741824
      └components: tracking_missile_sound_effect_manager
    missile_boom_sound (0,0) layer=1073741824
        └components: cc.AudioSource
  player_audio_manager (0,0) layer=1073741824
      └components: player_audio_manager
    fire_source (305,341) layer=128
        └components: cc.UITransform, cc.AudioSource
    land_source (305,341) layer=128
        └components: cc.UITransform, cc.AudioSource
    jump_source (305,341) layer=128
        └components: cc.UITransform, cc.AudioSource
  normal_one_fly_sound_effect_manager (0,0) layer=1073741824
      └components: normal_one_fly_sound_effect_manager
    激光枪射击_耳聆网_[声音ID：13323] (0,0) layer=1073741824
        └components: cc.AudioSource
    爆炸音效-像素破坏-怀旧游戏_爱给网_aigei_com (0,0) layer=1073741824
        └components: cc.AudioSource
  laser_sound_effect_manager (0,0) layer=1073741824
      └components: laser_sound_effect_manager
    激光的声音_耳聆网_[声音ID：10124] (0,0) layer=1073741824
        └components: cc.AudioSource
  circle_saw_sound_effect_manager (0,0) layer=1073741824
      └components: circle_saw_sound_effect_manager
    电锯切割到敌人 (0,0) layer=1073741824
        └components: cc.AudioSource
  warning_zone_sound_effect_manager (0,0) layer=1073741824
      └components: warning_zone_sound_effect_manager
    警示音效 (0,0) layer=1073741824
        └components: cc.AudioSource
  coin_sound_effect_manager (0,0) layer=1073741824
      └components: coin_sound_effect_manager
    拾取道具 (0,0) layer=1073741824
        └components: cc.AudioSource
  bullet_skill_ball_sound_effect_manager (0,0) layer=1073741824
      └components: bullet_skill_ball_sound_effect_manager
    补给箱放出补给的音效 (0,0) layer=1073741824
        └components: cc.AudioSource
  bullet_hit_player_sound_effect_manager (0,0) layer=1073741824
      └components: bullet_hit_player_sound_effect_manager
    角色被击中 (0,0) layer=1073741824
        └components: cc.AudioSource
  bullet_hit_enemy_sound_effect_manager (0,0) layer=1073741824
      └components: bullet_hit_enemy_sound_effect_manager
    玩家子弹命中 (0,0) layer=1073741824
        └components: cc.AudioSource

