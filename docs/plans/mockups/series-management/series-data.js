// 2025 reference configuration from issue #258. Mockup data only.
window.SERIES = {
  name: '2025 Vincent Hall Memorial Series',
  season: '1/1/2025 to 12/31/2025',
  isLocked: false,
  stages: [
    { kind: 'Qualifying', tournament: "Callahan's Bowl-O-Rama", dates: '1/24 to 1/25', month: 'Jan', count: 8, teamSize: 1 },
    { kind: 'Qualifying', tournament: 'Trios Team Event', dates: '2/21', month: 'Feb', count: 2, teamSize: 3 },
    { kind: 'Qualifying', tournament: 'NEBA Doubles, Baker Finals', dates: '2/22', month: 'Feb', count: 4, teamSize: 2 },
    { kind: 'Qualifying', tournament: 'Sparetime', dates: '3/22', month: 'Mar', count: 8, teamSize: 1 },
    { kind: 'Qualifying', tournament: 'BuddiesProShop.com Open', dates: '4/26', month: 'Apr', count: 8, teamSize: 1 },
    { kind: 'Qualifying', tournament: 'Westgate Lanes', dates: '5/31', month: 'May', count: 8, teamSize: 1 },
    { kind: 'Qualifying', tournament: 'Over 50 / Under Baker Doubles', dates: '6/13', month: 'Jun', count: 4, teamSize: 2 },
    { kind: 'Qualifying', tournament: 'Singles, Cranston', dates: '6/14', month: 'Jun', count: 8, teamSize: 1 },
    { kind: 'Survivor', tournament: "Callahan's Bowl-O-Rama", dates: '7/12', month: 'Jul', count: 40, teamSize: 1, squad: '9:00 AM squad on 7/12' },
    { kind: 'Survivor', tournament: "Lang's Singles Open", dates: '8/23', month: 'Aug', count: 20, teamSize: 1, squad: '9:00 AM squad on 8/23' },
    { kind: 'Placement', tournament: 'East Providence Singles', dates: '9/13', month: 'Sep', count: null, teamSize: 1 },
  ],
  // The Finale is its own event, not a Stage and not a tournament.
  finale: { count: 4, when: 'Sat 9/27 at 2:00 PM', host: "Callahan's Bowl-O-Rama, Seekonk, MA" },
};

// Display label for a Stage's advancing count (mirrors the planned SeriesDisplay formatter).
window.advancingLabel = function (s) {
  if (s.kind === 'Qualifying') {
    return s.teamSize > 1
      ? `Top ${s.count} teams<small class="adv-sub">(${s.count * s.teamSize} bowlers)</small>`
      : `Top ${s.count}`;
  }
  if (s.kind === 'Survivor') return `${s.count} advance`;
  const f = window.SERIES.finale;
  return f ? `Top ${f.count} to the Finale` : 'Final standings<small class="adv-sub">(no Finale)</small>';
};

// Most bowlers who can qualify: sum of Qualifying slots (teams count as team size).
window.qualifyingSlots = function (stages) {
  return stages.filter(s => s.kind === 'Qualifying').reduce((n, s) => n + s.count * s.teamSize, 0);
};

window.wireAdminToggle = function () {
  const toggle = document.getElementById('toggle-admin');
  if (toggle) toggle.addEventListener('change', e => document.body.classList.toggle('is-admin', e.target.checked));
  const lock = document.getElementById('toggle-locked');
  if (lock) lock.addEventListener('change', e => document.body.classList.toggle('is-locked', e.target.checked));
};
