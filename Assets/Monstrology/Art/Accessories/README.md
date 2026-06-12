# Accessories

Спрайты одежды для слотов `Head`, `Body` и `Legs`. Запись базы связывается через
`AccessoryData.id`.

Привязка едина для всех существ:

- `Head` -> `HeadAnchor`;
- `Body` -> `BodyAnchor`;
- `Legs` -> `LegAnchor`.

Позиция и размер берутся из `CreatureBaseTemplate`. Legacy-поля
`visualOffset` и `visualScale` сохранены для совместимости старых ассетов, но
стандартный гардероб их не использует.
