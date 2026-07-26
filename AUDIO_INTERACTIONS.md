# Аудиочекап взаимодействий

## База анализа

- Дата анализа: 2026-07-26.
- Git-база: `32b7863999b521e162aa3f3ae686c7cb2f9ac711` (`Add magic grab ability`).
- Проанализированы игровые скрипты, игровые сцены и префабы в `GMTK2026/Assets`.
- Учтены находившиеся на момент анализа незакоммиченные изменения механик импульса и нокаута. Они не создают отдельных новых типов звука сверх списка ниже.
- Сторонние demo-сцены и editor/test-only controls не входят в производственный список.
- В игровых сценах и префабах не найдено настроенных `AudioSource`/`AudioClip`. Пакет `Casual Game Sounds U6` присутствует, но его клипы к игровым событиям не подключены.

При следующем обновлении анализировать только коммиты после указанной Git-базы (`<base>..HEAD`) и незакоммиченные изменения, появившиеся после обновления базы. После обновления заменить Git-базу на новый `HEAD`. Уже перечисленные события повторно не добавлять.

## P0 — основные действия и обратная связь

| ID | Звуковое событие | Когда проигрывать | Варианты / примечания | Кодовая опора |
|---|---|---|---|---|
| `player_footstep` | Шаг/бег игрока | По footstep animation event, только при движении по земле | 3–6 вариантов; частота и громкость зависят от скорости | `TopDownController`: скорость и анимация движения |
| `dash_windup` | Подготовка рывка | При успешной активации Dash | Короткий anticipatory cue | `DashAbility.Activate/DashRoutine` |
| `dash_launch` | Старт рывка | После `windUpTime`, одновременно с dash particles | Свист/рывок; не смешивать с wind-up | `DashAbility.DashRoutine` |
| `punch_combo_start` | Начало серии ударов | При успешном нажатии и входе в combo | Короткий замах | `PunchComboAbility.Activate` |
| `punch_combo_hit` | Попадания серии | При фактическом контакте цели в активном punch trigger / нанесении накопленного урона | Несколько вариантов; ограничить частоту, поскольку урон тиковый | `PunchComboTrigger.OnTriggerEnter/Update` |
| `punch_combo_miss` | Удары по воздуху | Во время combo-анимации, если на конкретном ударном кадре нет цели | Лучше вызывать animation events | `PunchComboAbility`, animator `Combo` |
| `punch_combo_final_hit` | Финальный удар | При отпускании кнопки, если в trigger есть цель | Усиленный impact + запуск цели | `PunchComboAbility.Deactivate`, `PunchComboTrigger.FinalHit` |
| `impulse_cast` | Каст импульсного заклинания | При успешной активации / начале wind-up | Магический заряд | `ImpulseSpellAbility.CastRoutine` |
| `impulse_projectile_launch` | Вылет импульсного снаряда | В момент `SpawnProjectile` | Короткий launch; можно объединить с cast при малом wind-up | `ImpulseSpellAbility.SpawnProjectile` |
| `impulse_projectile_fly_loop` | Полёт импульсного снаряда | Пока снаряд существует | Тихий 3D loop, необязателен | `ExplosiveProjectile` |
| `impulse_explosion` | Взрыв снаряда | При первом столкновении | Главный взрывной impact | `ExplosiveProjectile.Explode` |
| `magic_grab_launch` | Бросок магической перчатки | При успешной активации | Выстрел/растяжение связи | `MagicGrabAbility.Activate` |
| `magic_grab_fly_loop` | Полёт перчатки | От запуска до попадания/промаха | Короткий loop, необязателен | `MagicGrabProjectile.FixedUpdate` |
| `magic_grab_latch_movable` | Захват подвижной цели | Перчатка попала в доступный `ImpulseReceiver` | Мягкий магический захват | `MagicGrabAbility.OnProjectileHit/BeginTargetOrbit` |
| `magic_grab_latch_anchor` | Зацеп за неподвижную поверхность | Попадание без доступной подвижной цели | Более твёрдый hook/impact | `MagicGrabAbility.OnProjectileHit/BeginPlayerOrbit` |
| `magic_grab_miss` | Перчатка не попала | Достигнута максимальная дистанция | Короткий retract/fizzle | `MagicGrabProjectile` → `OnProjectileMissed` |
| `magic_grab_orbit_loop` | Удержание и вращение связки | Пока цель или игрок вращается вокруг якоря | Один управляемый loop; pitch от скорости | `MagicGrabAbility.FixedUpdate` |
| `magic_grab_release` | Ручное отпускание с инерцией | Повторное нажатие или переключение на другую способность | Snap/release + бросок | `MagicGrabAbility.CancelGrab(true)` |
| `magic_grab_break` | Вынужденный обрыв связи | Урон, нокаут, смерть, исчезновение якоря или disable | Вариант «оборвалось», без звука броска при `applyInertia=false` | `MagicGrabAbility.CancelGrab`, interruption handlers |
| `character_damage` | Получение урона | На `HealthComponent.onDamaged` | Разделить игрока, врага и объект; не дублировать с contact impact слишком громко | `HealthComponent.TakeDamage` |
| `character_death` | Смерть обычного врага | На `HealthComponent.onDeath` / появлении death VFX | Отдельно от разрушения props | `DummyEnemy.HandleDeath` |

## P1 — физика и интерактивные объекты

| ID | Звуковое событие | Когда проигрывать | Варианты / примечания | Кодовая опора |
|---|---|---|---|---|
| `impulse_received` | Объект получил импульс | Один раз на принятый ненулевой импульс | Whoosh/launch; сила управляет громкостью/pitch | `ImpulseReceiver.ApplyImpulse`, уже есть события `ImpulseReceived` и `onImpulseReceived` |
| `impulse_collision_movable` | Летящий объект ударил подвижный объект | При первой передаче импульса данной паре объектов | Материальные варианты; сила от `currentSpeed` | `ImpulseReceiver.TransferImpulse` |
| `impulse_collision_solid` | Летящий объект ударил стену/неподвижный объект | Перед `StopAndLock` | Тупой impact; сила от скорости | `ImpulseReceiver.OnCollisionEnter` |
| `breakable_prop_damage` | Повреждение разрушаемого предмета | На валидном collision damage или другом уроне | Скрип/трещина; несколько степеней при наличии HP | `BreakableProp.OnCollisionEnter/OnDamaged` |
| `breakable_prop_break` | Разрушение предмета | На `onDeath`, до задержанного Destroy | Отдельный набор по материалу | `BreakableProp.OnDeath` |
| `training_stand_player_hit` | Игрок раскрутил тренировочный стенд | Только при валидном столкновении игрока выше `minPlayerSpeed` | Металлический/деревянный удар + старт вращения | `TrainingStand.OnArmCollision` |
| `training_stand_damage_hit` | Стенд получил урон/импульс и ускорился | На `onDamaged` или `ApplyImpulseSpin` | Удар по корпусу | `TrainingStand.OnDamaged/ApplyImpulseSpin` |
| `training_stand_spin_loop` | Вращение стенда | Пока угловая скорость выше небольшого порога | Pitch/volume от angular velocity | `TrainingStand`, Rigidbody angular velocity |
| `training_stand_arm_impact` | Вращающаяся рука попала по персонажу/объекту | Один раз на `OnCollisionEnter`, когда скорость выше `spinThreshold` | Impact + отбрасывание; не каждый physics frame | `TrainingStand.OnArmCollision` |
| `training_stand_destroy` | Разрушение стенда | На `onDeath` перед Destroy | Крах механизма | `TrainingStand.OnDeath` |
| `fall_trap_trigger` | Персонаж провалился в яму | При первом входе объекта с `HealthComponent` | Падение/крик/низкий whoosh | `FallTrap.OnTriggerEnter` |

## P1 — нокаут и UI

| ID | Звуковое событие | Когда проигрывать | Варианты / примечания | Кодовая опора |
|---|---|---|---|---|
| `star_collected` | Звезда добавлена и долетела до счётчика | Лучше в момент достижения счётчика, а не в момент `AddStars` | При нескольких звёздах допустима восходящая серия | `KnockoutSystem.AnimateCollectedStar` |
| `knockout_start` | Начало нокаута | Однократно при `onKnockoutStarted` | Сильный sting, мир ставится на паузу | `KnockoutSystem.KnockoutSequence` |
| `knockout_stars_scatter` | Звёзды разлетаются из счётчика | При запуске scatter; при множестве — один burst или короткая россыпь | Не запускать полный звук на каждую звезду одновременно | `KnockoutSystem.ScatterStars` |
| `knockout_star_click` | Игрок нажал разбросанную звезду | На успешном удалении активной звезды | Короткий UI pop; можно повышать pitch по прогрессу | `KnockoutSystem.RemoveKnockoutStar` |
| `knockout_countdown_tick` | Тик таймера | Только при смене отображаемого целого числа | Ускорение уже задаётся `TimerSpeed`; последние 3 тика усилить | `KnockoutSystem.KnockoutSequence` |
| `knockout_recovered` | Все звёзды собраны, игрок встал | На `onRecovered` | Позитивный recovery sting | `KnockoutSystem.Recover` |
| `game_over` | Время вышло | На `onGameOver` | Отрицательный финальный sting | `KnockoutSystem.GameOver` |
| `ui_button_hover` | Наведение/выбор активной кнопки | На pointer enter / selection | Для меню и будущего UI | `MenuScene`, Unity UI Button |
| `ui_button_press` | Нажатие кнопки | На pointer down | Короткий tactile click | `MenuScene`, `CustomButton`/Unity UI Button |
| `ui_button_confirm` | Успешное действие кнопки | На валидном `onClick` | Сейчас PLAY-кнопка визуально существует, но её `onClick` пуст | `MenuScene` |

## P2 — полезные дополнительные слои

| ID | Звуковое событие | Когда проигрывать | Примечание |
|---|---|---|---|
| `punch_target_lift` | Цель поднята комбо | При первом входе цели в combo trigger | Магический/аркадный подъём, если это читается как отдельное действие |
| `magic_grab_orbit_collision` | Вращаемая цель задевает другой объект | На разрешённый collision impulse с встроенным cooldown 0.15 с | Можно использовать вариант общего physics impact |
| `movement_start_stop` | Старт/остановка быстрого бега | При переходе порога run particles | Нужен только если шагов недостаточно для ощущения веса |
| `invulnerability_end` | Закончилась неуязвимость после подъёма | В момент окончания `invulnerableUntil` | Сейчас нет отдельного события/визуального перехода; потребует нового хука |

## Правила интеграции

- Разделять **action** и **result**: каст/замах звучит всегда после успешной активации, hit — только при реальном попадании.
- Для тикового урона комбо не запускать звук из каждого `TakeDamage`; привязать удары к animation events или поставить sound cooldown.
- У импульсных столкновений масштабировать громкость/вариант по силе или скорости и защищаться от повторов одной пары коллайдеров.
- Loop-звуки (полёт, вращение, grab link) должны гарантированно останавливаться при Destroy, disable, смерти, нокауте и отмене способности.
- UI нокаута работает при `Time.timeScale = 0`, поэтому его звук и анимационные задержки не должны зависеть от scaled time.
- Для шагов и ударов подготовить несколько вариантов с небольшим random pitch, чтобы частые события не звучали механически.
