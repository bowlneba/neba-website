/* Sample data for the defending champions mockups. The Jan 2027 tournaments are scheduled, so they show defenders. */
window.DEFENDING_FORMATS = {
  singles: {
    label: 'Singles',
    tournament: 'NEBA Singles',
    date: 'Sat, Jan 9, 2027',
    place: 'Lucky Strike Lanes, Waltham MA',
    fee: '$95 per bowler',
    teamSize: 1,
    current: { season: '2026', names: ['Dave Smith'] },
    older: { season: '2025', names: ['Linda Chu'] },
    winners2027: ['Maria Santos'],
  },
  doubles: {
    label: 'Doubles',
    tournament: "Joe's Pizza Open",
    date: 'Sat, Jan 9, 2027',
    place: 'Lucky Strike Lanes, Waltham MA',
    fee: '$85 per bowler',
    teamSize: 2,
    current: { season: '2026', names: ['Mike Rossi', 'Tom Anders'] },
    older: { season: '2025', names: ['Chris Martin', 'Ann Whitaker'] },
    winners2027: ['Maria Santos', 'Paul Ouellette'],
  },
  trios: {
    label: 'Trios',
    tournament: 'Harbor Trios',
    date: 'Sat, Feb 6, 2027',
    place: 'Bowl-O-Rama, Quincy MA',
    fee: '$75 per bowler',
    teamSize: 3,
    current: { season: '2026', names: ['Mike Rossi', 'Tom Anders', 'Linda Chu'] },
    older: { season: '2025', names: ['Chris Martin', 'Ann Whitaker', 'Paul Ouellette'] },
    winners2027: ['Maria Santos', 'Dave Smith', 'John Smith'],
  },
  baker: {
    label: 'Baker',
    tournament: 'NEBA Baker Open',
    date: 'Sat, Mar 13, 2027',
    place: 'Sparetime Recreation, Worcester MA',
    fee: '$60 per bowler',
    teamSize: 5,
    current: { season: '2026', names: ['Mike Rossi', 'Tom Anders', 'Linda Chu', 'Dave Smith', 'Ann Whitaker'] },
    older: { season: '2025', names: ['Chris Martin', 'Paul Ouellette', 'Maria Santos', 'John Smith', 'Christine Marsh'] },
    winners2027: ['Maria Santos', 'Dave Smith', 'John Smith', 'Chris Martin', 'Linda Chu'],
  },
};

/** Defending teams as the API sends them: one team, or two when the previous tournament was truncated. */
window.defendingTeams = function (f, truncated) {
  const first = { season: f.current.season, names: f.current.names, truncated: truncated, undefended: false };
  return truncated ? [first, { season: f.older.season, names: f.older.names, truncated: false, undefended: true }] : [first];
};
