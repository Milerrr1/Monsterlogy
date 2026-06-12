# Monstrology Art

```text
Art/
├── Creatures/
├── Evolutions/
├── Accessories/
├── Biomes/
├── Items/
├── UI/
└── Resources/
```

Рабочие компоненты получают графику через
`Resources/SpriteDatabase.asset`. Записи существ, предметов, одежды, логовищ и
событий связываются по стабильному `id`. Замена ссылки на спрайт не требует
изменения игрового кода или сохранений.

Единый стандарт существ хранится в
`Resources/CreatureBaseTemplate.asset`. Открыть ассеты можно через:

- `Tools > Monstrology > Open Sprite Database`
- `Tools > Monstrology > Open Creature Base Template`

Проверка выполняется в Play Mode командой
`Tools > Monstrology > Validate Creature Template`.
