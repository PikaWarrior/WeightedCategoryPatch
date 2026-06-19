# WeightedCategoryPatch

A lightweight patch for **BrutalCompanyMinusExtraReborn** that improves event selection when `Use custom weights?` is enabled.

## What it does

By default, enabling `Use custom weights?` removes category separation entirely - all events are merged into one large pool and selected purely by their individual weights, ignoring type distribution.

This patch adds **category-aware weighted selection**: when choosing an event, the mod first picks an event *category* (Insane, VeryBad, Bad, Neutral, Good, VeryGood, Rare, Remove) according to the configured type weights - just like it works with `Use custom weights?` disabled - and then picks a random event within that category taking its individual weight into account. This makes the type weight configuration actually meaningful.

## Requirements

- [BrutalCompanyMinusExtraReborn](https://thunderstore.io/c/lethal-company/p/SoftDiamond/BrutalCompanyMinusExtraReborn)
- BrutalCompanyMinusExtraReborn → `Difficulty_Settings.cfg` → **[_Event Settings]** → `Use custom weights? = true`
  > ⚠️ Without this option enabled, the patch does nothing.

## Compatibility

Only affects event selection logic when `Use custom weights?` is enabled. All other BrutalCompanyMinusExtraReborn functionality remains unchanged.
