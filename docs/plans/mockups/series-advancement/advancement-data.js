// Synthetic advancement data for the mockups. Invented bowlers. Uses the 2025 Stage layout from phase 1.
// Three scenarios: mid (season under way), squad (a Survivor round waits for a squad), end (all computed).
(function () {
  const F = ['Marcus', 'Dana', 'Priya', 'Tom', 'Elena', 'Jorge', 'Ruth', 'Kenji', 'Sam', 'Alicia', 'Nate', 'Maria', 'Owen', 'Leah', 'Victor', 'Hannah', 'Ray', 'Tessa', 'Ian', 'Carla', 'Dmitri', 'Joan', 'Luis', 'Mina', 'Paul', 'Bree', 'Omar', 'Claire', 'Walt', 'Yuki'];
  const L = ['Alvarez', 'Brennan', 'Castellano', 'Doyle', 'Ferreira', 'Gallagher', 'Hartwell', 'Iverson', 'Jankowski', 'Kowalczyk', 'Lombardi', 'Mercer', 'Nakamura', 'Okafor', 'Pelletier', 'Quinlan', 'Rowe', 'Sorensen', 'Tran', 'Underhill', 'Vasquez', 'Whitaker', 'Yoon', 'Zielinski', 'Abernathy', 'Boudreau', 'Caron', 'Dufresne', 'Eastman', 'Fitzgerald'];
  const nm = i => `${F[i % 30]} ${L[(i * 7 + 3 + (i >= 30 ? 5 : 0)) % 30]}`;

  const VACANT = [0, 1, 1, 2, 0, 1, 0, 1]; // vacant slots per Qualifying Stage (S1 to S8)

  function build(key) {
    const base = window.SERIES.stages;
    let cursor = 0;
    const take = n => Array.from({ length: n }, () => nm(cursor++));
    const stages = base.map((s, i) => ({
      order: i + 1, kind: s.kind, tournament: s.tournament, dates: s.dates, teamSize: s.teamSize, count: s.count,
      squadDescription: s.squad ?? null,
      status: 'Pending', isStale: false, needsSquad: false, officialDueAt: null, tournamentStatus: 'Scheduled',
      slotCount: s.kind === 'Qualifying' ? s.count * s.teamSize : null, vacant: 0,
      entries: [], overrides: [], outcomes: [], squadOptions: [], squadResolved: null,
    }));

    function fillQualifying(i, official) {
      const st = stages[i];
      const filled = st.slotCount - VACANT[i];
      const names = take(filled);
      st.entries = names.map((b, k) => ({ bowler: b, place: st.teamSize > 1 ? Math.floor(k / st.teamSize) + 1 : k + 1, source: 'Qualifier', points: null }));
      st.vacant = VACANT[i];
      st.tournamentStatus = 'Completed';
      st.status = official ? 'Official' : 'Computed';
    }

    const adminAdd = { kind: 'Add', bowler: null, reason: 'Walk-in registration confirmed by the tournament director.', date: '3/23/2025', applied: true };
    const adminClear = { kind: 'Clear', bowler: null, reason: 'Membership lapsed before the event; confirmed with the membership secretary.', date: '3/23/2025', applied: true, place: 7 };

    if (key === 'mid') {
      [0, 1, 2].forEach(i => fillQualifying(i, true));
      stages[2].isStale = true;
      fillQualifying(3, false);
      stages[3].officialDueAt = '3:15 PM on 3/23';
      // S4: one admin Add, one admin Clear.
      const added = nm(cursor++);
      const cleared = nm(cursor++);
      stages[3].entries.push({ bowler: added, place: 9, source: 'Admin', points: null });
      stages[3].overrides = [{ ...adminAdd, bowler: added }, { ...adminClear, bowler: cleared }];
      stages[6].tournamentStatus = 'Cancelled';
      return { version: 14, backfillAsOf: null, hasFinale: true, stages, positions: [] };
    }

    // squad and end: Qualifying Stages official, Backfill has run.
    for (let i = 0; i < 8; i++) fillQualifying(i, true);
    const added = nm(cursor++);
    const cleared = nm(cursor++);
    stages[3].entries.push({ bowler: added, place: 9, source: 'Admin', points: null });
    stages[3].overrides = [{ ...adminAdd, bowler: added }, { ...adminClear, bowler: cleared }];
    // The Backfill belongs to the last Qualifying Stage (S8). Need = target - entries.
    const target = stages.filter(s => s.kind === 'Qualifying').reduce((n, s) => n + s.slotCount, 0);
    const have = stages.reduce((n, s) => n + s.entries.length, 0);
    const need = target - have;
    const backfill = take(need).map((b, k) => ({ bowler: b, place: null, source: 'Backfill', points: 520 - k * 35 }));
    stages[7].entries.push(...backfill);
    const data = { version: 31, backfillAsOf: '6/14/2025', hasFinale: true, stages, positions: [] };

    const field = () => stages.filter(s => s.kind === 'Qualifying').flatMap(s => s.entries.map(e => e.bowler));
    if (key === 'squad') {
      stages[8].tournamentStatus = 'Completed';
      stages[8].needsSquad = true;
      stages[8].squadOptions = [
        { id: 'a', label: '9:30 AM on 7/12', hasScores: true, current: false },
        { id: 'b', label: '1:00 PM on 7/12', hasScores: true, current: false },
      ];
      return data;
    }
    // end
    window.ADV_DATA.computeSurvivor(data, 9, true);
    window.ADV_DATA.computeSurvivor(data, 10, false);
    stages[10].tournamentStatus = 'Completed';
    stages[10].status = 'Computed';
    const s10 = stages[9].outcomes.filter(o => o.advanced).map(o => o.bowler);
    data.positions = s10.map((b, k) => ({ bowler: b, position: k + 1, bowled: k < s10.length - 2, finalist: k < 4 }));
    // Bowlers who did not bowl rank last.
    void field;
    return data;
  }

  // Block totals are invented: a fixed spread so the cut and ties look real.
  function score(i, round) { return 1490 - ((i * 17 + round * 11) % 53) * 6 - (i % 4) * 3; }

  window.ADV_DATA = {
    build,
    // Compute a Survivor Stage's outcomes from the field before it.
    computeSurvivor(data, order, official) {
      const st = data.stages[order - 1];
      const prev = data.stages.slice(0, order - 1).reverse().find(s => s.kind === 'Survivor');
      const names = prev ? prev.outcomes.filter(o => o.advanced).map(o => o.bowler)
        : data.stages.filter(s => s.kind === 'Qualifying').flatMap(s => s.entries.map(e => e.bowler));
      const rows = names.map((b, i) => ({ bowler: b, block: (i % 11 === 10 && order === 9) ? null : score(i, order) }));
      rows.sort((a, b) => (b.block ?? -1) - (a.block ?? -1));
      const bowled = rows.filter(r => r.block !== null).length;
      const cut = rows[Math.min(st.count, bowled) - 1].block;
      st.outcomes = rows.map(r => ({ bowler: r.bowler, block: r.block, advanced: r.block !== null && r.block >= cut }));
      st.tournamentStatus = 'Completed';
      st.needsSquad = false;
      st.status = official ? 'Official' : 'Computed';
      st.officialDueAt = official ? null : '3:15 PM on 8/24';
    },
    // Result rows for the Add bowler picker: finishers outside the slots.
    resultRows(stage) {
      const taken = new Set([...stage.entries.map(e => e.bowler), ...stage.overrides.map(o => o.bowler)]);
      return Array.from({ length: 14 }, (_, k) => ({ bowler: nm(70 + k * 3), place: stage.slotCount + k + 1 })).filter(r => !taken.has(r.bowler));
    },
  };
})();
