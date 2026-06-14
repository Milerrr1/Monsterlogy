# Creatures

Каждое существо рисуется фронтально, без поворота и перспективы, с симметричным
силуэтом и одинаковыми пропорциями:

```text
Голова: 40%
Тело:   40%
Ноги:   20%
```

Требования к файлу:

- холст `2048x2048`;
- прозрачный фон;
- персонаж по центру;
- по `10%` прозрачного отступа с каждой стороны;
- pivot строго в центре.

В `CreatureData` доступны `portraitSprite`, `worldSprite` и необязательный
`evolutionSprite`. Те же ссылки можно переопределить по стабильному
`CreatureData.id` в списке `Creature Visuals` базы `SpriteDatabase`.

Поле `icon` остаётся резервным источником для старых ассетов. Runtime-объекты
используют `CreatureVisualRig`, который автоматически создаёт `Visual`,
`HeadAnchor`, `BodyAnchor` и `LegAnchor`.

Первые финальные спрайты:

- `Breadcat.png`: `bread_cat`, `bread_cat_ii`, `bread_cat_iii`,
  `bread_cat_king`;
- `VacuumRhino.png`: `vacuum_rhino`, `vacuum_rhino_ii`,
  `vacuum_rhino_iii`, `turbo_vacuum_rhino`.

До появления отдельных эволюционных изображений формы используют спрайт базового
вида через `SpriteDatabase`.
