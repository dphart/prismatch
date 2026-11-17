'use strict';

const assert = require('node:assert/strict');
const M = require('./model.js');
const R = require('./round-offers.js');
const catalog = require('../../Assets/Resources/Content/prismatch.json');

function check(name, included, excluded = []) {
 const power = catalog.powers.find(power => power.name === name);
 assert(power, name);
 const qualities = M.qualities(power);
 for (const key of included) assert(qualities.has(key), `${name} should match ${key}`);
 for (const key of excluded) assert(!qualities.has(key), `${name} should not match ${key}`);
}

check('Shore Sweep', ['clearing', 'base', 'edge']);
check('Bear Collector', ['credits', 'base', 'animal:Bear'], ['clearing']);
check('Square Dividend', ['credits', 'base', 'square'], ['clearing']);
check('Clean Slate', ['credits', 'base', 'shuffle'], ['clearing']);
check('Duck + Walrus Pact', ['matching', 'tradeoff'], ['factor', 'credits', 'clearing']);
check('Welcome Bear', ['factor', 'credits', 'roster', 'tradeoff'], ['clearing']);
check('Farewell Bear', ['roster', 'tradeoff'], ['factor', 'credits', 'clearing']);
check('Bear Magnet', ['spawn', 'animal:Bear'], ['roster']);
check('Bear Edge Bloom', ['transform', 'edge', 'animal:Bear'], ['clearing']);
check('Bear Square Bloom', ['transform', 'square', 'animal:Bear']);
check('Bear Giant Bloom', ['transform', 'minimum', 'animal:Bear']);
check('Edge Balance', ['edge', 'cascade', 'growth'], ['corner']);
check('Consistency Is Key', ['sequence', 'progress', 'multi'], ['growth']);
check('Menagerie Bounty', ['animal', 'sequence']);
check('Menagerie Engine', ['animal', 'sequence', 'progress', 'growth'], ['cascade']);
check('Smaller is Better', ['small'], ['minimum']);
check('Big Game', ['minimum']);
check('Plus Architect', ['plus']);
check('Horizontal Dividend', ['line', 'credits']);
check('Four is Good', ['exact', 'multi'], ['base']);
check('Stacked Odds', ['exact', 'base'], ['minimum']);

const groups = M.filterGroups;
assert(groups.find(group => group.name === 'Where it appears').items.some(([key]) => key === 'points'));
assert(groups.find(group => group.name === 'Where it appears').items.some(([key]) => key === 'blocks'));
const visibleKeys = groups.flatMap(group => group.items.map(([key]) => key));
assert.equal(new Set(visibleKeys).size, visibleKeys.length);
for (const key of visibleKeys) assert(catalog.powers.some(power => M.qualities(power).has(key)), `${key} has no matches`);
for (const power of catalog.powers) {
 assert.deepEqual(power.qualities, [...M.qualities(power)].sort(), `${power.name} has stale runtime filters`);
}
assert.deepEqual(catalog.offerFilterKeys, R.filterKeys(catalog.powers));

const credits = {source:'custom', goals:'Blocks', reward:'credits', trigger:'edge', animal:'Any'};
assert(M.qualities(credits).has('credits'));
assert(M.qualities(credits).has('tag:GoalCredit'));
assert(!M.qualities(credits).has('tag:Multiplier'));
assert(!M.qualities({source:'game', goals:'Points', description:'Cascades do not count.'}).has('cascade'));
console.log(`PASS filter categories, positive rewards, conditions and runtime metadata for ${catalog.powers.length} powers`);
