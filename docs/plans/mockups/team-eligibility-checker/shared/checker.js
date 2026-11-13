/* Throwaway scripting for the checker mockups. Simulates search, slots, and results. Not the real implementation.
   The page sets <body data-option="1|2|3"> to pick how a "bowler not found in system" is entered. */
(function () {
  const OPTION = document.body.dataset.option;

  const FORMATS = {
    doubles: { label: 'Doubles', size: 2, tournament: "Joe's Pizza Open", dates: 'Jan 9 to 10, 2027' },
    overunder: { label: 'Over/Under 50 Doubles', size: 2, tournament: 'Winter Over/Under', dates: 'Jan 16 to 17, 2027' },
    trios: { label: 'Trios', size: 3, tournament: 'Harbor Trios', dates: 'Feb 6, 2027' },
    baker: { label: 'Baker', size: 5, tournament: 'NEBA Baker Open', dates: 'Mar 13, 2027' },
  };

  const RULES = {
    doubles: `
      <h2>Team rules for doubles</h2>
      <h3>Team composition</h3>
      <ul>
        <li>Entries are two-person teams. Each person pays the entry fee.</li>
        <li>Only one previous champion is permitted per team.</li>
        <li>When two or more tournaments are on the same weekend, champions crowned earlier that weekend do not count. A non-champion who wins may still bowl with a champion for the rest of the weekend.</li>
        <li>Teams with an invalid configuration will be disqualified.</li>
      </ul>
      <h3>Champion definition</h3>
      <p>A champion is a NEBA title or professional (PBA/PWBA National or Regional) title holder whose last title was within ten years. Bowlers who have turned 60 count titles from the last five years only.</p>`,
    overunder: `
      <h2>Team rules for over/under doubles</h2>
      <h3>Team composition</h3>
      <ul>
        <li>Entries are two-person teams. Each person pays the entry fee.</li>
        <li>One partner must be 50 or older and one must be under 50, each as of the date of the squad the team bowls in.</li>
        <li>Only one previous champion is permitted per team.</li>
        <li>Teams with an invalid configuration will be disqualified.</li>
      </ul>
      <h3>Champion definition</h3>
      <p>A champion is a NEBA title or professional (PBA/PWBA National or Regional) title holder whose last title was within five years.</p>`,
    trios: `
      <h2>Team rules for trios</h2>
      <h3>Team composition</h3>
      <ul>
        <li>Entries are three-person teams.</li>
        <li>A team needs at least one non-champion and may have at most one multi-time champion.</li>
      </ul>
      <h3>Champion definition</h3>
      <ul>
        <li>One-time champion: won a single NEBA or professional title in the last five years.</li>
        <li>Multi-time champion: won more than one NEBA and/or professional title in the last five years.</li>
        <li>Non-champion: does not meet either description.</li>
      </ul>`,
    baker: `
      <h2>Team rules for baker</h2>
      <h3>Team composition</h3>
      <ul>
        <li>Entries are five-person teams.</li>
        <li>At most two multi-time champions.</li>
        <li>At least one non-champion.</li>
        <li>At most one NEBA Hall of Fame member for Superior Performance.</li>
      </ul>
      <h3>Champion definition</h3>
      <ul>
        <li>One-time champion: won a single NEBA or professional title in the last five years.</li>
        <li>Multi-time champion: won more than one NEBA and/or professional title in the last five years.</li>
        <li>Non-champion: does not meet either description.</li>
      </ul>`,
  };

  const BOWLERS = [
    { id: 'b1', name: 'Dave Smith', usbc: '4821-09317' },
    { id: 'b2', name: 'David Smithson', usbc: '3377-55120' },
    { id: 'b3', name: 'John Smith', usbc: '1102-48871' },
    { id: 'b4', name: 'John Smith', usbc: '5530-20914' },
    { id: 'b5', name: 'Maria Santos', usbc: '2290-61045' },
    { id: 'b6', name: 'Mike Rossi', usbc: '6671-30288' },
    { id: 'b7', name: 'Tom Anders', usbc: '7415-23890' },
    { id: 'b8', name: 'Linda Chu', usbc: '8054-71163' },
    { id: 'b9', name: 'Chris Martin', usbc: '9012-44630' },
    { id: 'b10', name: 'Christine Marsh', usbc: '9120-17745' },
    { id: 'b11', name: 'Paul Ouellette', usbc: '3340-92016' },
    { id: 'b12', name: 'Ann Whitaker', usbc: '4467-05582' },
  ];

  const RESULTS = {
    eligible: `
      <section class="result result--ok" aria-live="polite">
        <div class="headline"><span class="material-symbols-outlined" aria-hidden="true">check_circle</span>This team can bowl together.</div>
        <p>We checked NEBA titles, age, and Hall of Fame membership. Everyone qualifies.</p>
        <p class="note">We can't check PBA or PWBA titles. Make sure no one on the team has a professional title inside the champion window above.</p>
      </section>`,
    defending: `
      <section class="result result--defend" aria-live="polite">
        <div class="headline"><span class="material-symbols-outlined" aria-hidden="true">shield</span>These are the defending champions.</div>
        <p>This team won the 2026 Joe's Pizza Open, so the champion limits do not apply.</p>
      </section>`,
    undefended: `
      <section class="result result--defend" aria-live="polite">
        <div class="headline"><span class="material-symbols-outlined" aria-hidden="true">shield</span>These are the defending champions.</div>
        <p>This team won the 2025 Joe's Pizza Open. The 2026 tournament was truncated, so they never got to defend. The champion limits do not apply.</p>
      </section>`,
    champions: `
      <section class="result result--no" aria-live="polite">
        <div class="headline"><span class="material-symbols-outlined" aria-hidden="true">block</span>This team can't bowl together.</div>
        <ul><li>A doubles team can have at most one previous champion. This team has two.</li></ul>
        <p class="contact">If you think this is a mistake, email <a href="mailto:info@bowlneba.com">info@bowlneba.com</a>.</p>
      </section>`,
    age: `
      <section class="result result--no" aria-live="polite">
        <div class="headline"><span class="material-symbols-outlined" aria-hidden="true">block</span>This team can't bowl together.</div>
        <ul><li>We could not verify the age of every bowler. An over/under team needs every bowler found in our system with an age we can check.</li></ul>
        <p class="contact">If you think this is a mistake, email <a href="mailto:info@bowlneba.com">info@bowlneba.com</a>.</p>
      </section>`,
    twoover: `
      <section class="result result--no" aria-live="polite">
        <div class="headline"><span class="material-symbols-outlined" aria-hidden="true">block</span>This team can't bowl together.</div>
        <ul><li>An over/under team needs one bowler in each age group. Both bowlers on this team are in the same group.</li></ul>
        <p class="contact">If you think this is a mistake, email <a href="mailto:info@bowlneba.com">info@bowlneba.com</a>.</p>
      </section>`,
    date: `
      <section class="result result--date" aria-live="polite">
        <div class="headline"><span class="material-symbols-outlined" aria-hidden="true">event</span>The answer depends on the squad date.</div>
        <p>This team can bowl together on some days of the tournament but not others.</p>
        <div class="dates">
          <div class="row good"><span class="when">Sat, Jan 16, 2027</span><span class="verdict">Can bowl together</span></div>
          <div class="row bad"><span class="when">Sun, Jan 17, 2027</span><span class="verdict">Can't bowl together. Both bowlers are in the same age group on this date.</span></div>
        </div>
        <p class="note">We can't check PBA or PWBA titles. Make sure no one on the team has a professional title inside the champion window.</p>
        <p class="contact">If you think this is a mistake, email <a href="mailto:info@bowlneba.com">info@bowlneba.com</a>.</p>
      </section>`,
    unknown: `
      <section class="result result--no" aria-live="polite">
        <div class="headline"><span class="material-symbols-outlined" aria-hidden="true">block</span>This team can't bowl together.</div>
        <ul><li>A baker team can have at most one NEBA Hall of Fame member for Superior Performance. This team has two.</li></ul>
        <p class="note">Bowlers who are not in our system count as having no NEBA titles.</p>
        <p class="contact">If you think this is a mistake, email <a href="mailto:info@bowlneba.com">info@bowlneba.com</a>.</p>
      </section>`,
  };

  const state = { format: 'doubles', slots: [], unknown: 0, result: 'eligible' };

  const $ = (sel) => document.querySelector(sel);
  const esc = (s) => s.replace(/[&<>"]/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[c]));

  function reset() {
    const size = FORMATS[state.format].size;
    state.slots = Array.from({ length: size }, () => ({ bowler: null, unknown: false }));
    state.unknown = 0;
    $('#result').innerHTML = '';
    render();
  }

  function chosenIds() { return state.slots.filter((s) => s.bowler).map((s) => s.bowler.id); }

  function isComplete() {
    if (OPTION === '2') return state.slots.slice(0, FORMATS[state.format].size - state.unknown).every((s) => s.bowler);
    return state.slots.every((s) => s.bowler || s.unknown);
  }

  function renderHead() {
    const f = FORMATS[state.format];
    $('#head').innerHTML = `
      <h1>Check team eligibility</h1>
      <div class="sub"><span class="name">${esc(f.tournament)}</span><span class="chip">${esc(f.label)}</span><span>${esc(f.dates)}</span></div>`;
    $('#rules').innerHTML = RULES[state.format];
  }

  function slotHtml(i) {
    const slot = state.slots[i];
    const label = `Bowler ${i + 1}`;
    const id = `slot-${i}`;

    if (OPTION === '2') {
      const real = FORMATS[state.format].size - state.unknown;
      if (i >= real) {
        return `<div class="slot"><span class="slot-label">${label}</span><div class="unknown-pill"><span class="tag"><span class="material-symbols-outlined" aria-hidden="true">person_off</span>Not in our system</span></div></div>`;
      }
    }

    if (slot.unknown) {
      return `<div class="slot"><span class="slot-label">${label}</span>
        <div class="unknown-pill"><span class="tag"><span class="material-symbols-outlined" aria-hidden="true">person_off</span>Not in our system</span>
        <button type="button" class="link-btn" data-undo="${i}">Undo</button></div></div>`;
    }

    let extra = '';
    if (OPTION === '1') {
      extra = `<div class="slot-extra"><label><input type="checkbox" data-unknown="${i}"> Bowler not found in system</label></div>`;
    } else if (OPTION === '3') {
      extra = `<div class="slot-extra">Can't find them? <button type="button" class="link-btn" data-mark="${i}">Mark as not in our system</button></div>`;
    }

    const value = slot.bowler ? `${slot.bowler.name} (${slot.bowler.usbc})` : '';
    return `<div class="slot">
      <label for="${id}">${label}</label>
      <div class="ac" data-ac="${i}">
        <input id="${id}" class="input" type="text" role="combobox" aria-expanded="false" autocomplete="off"
               placeholder="Type at least 3 letters of a name" value="${esc(value)}" ${slot.bowler ? 'readonly' : ''}>
        ${slot.bowler ? `<button type="button" class="clear" aria-label="Clear bowler ${i + 1}" data-clear="${i}">&times;</button>` : ''}
        <div class="ac-panel" hidden></div>
      </div>
      ${extra}
    </div>`;
  }

  function render() {
    renderHead();
    const f = FORMATS[state.format];
    let stepper = '';
    if (OPTION === '2') {
      stepper = `
        <div class="stepper-card">
          <div class="text"><strong>Bowlers not in our system</strong><span>Add one for each bowler who has never bowled NEBA or doesn't show up in search.</span></div>
          <div class="stepper" role="group" aria-label="Bowlers not in our system">
            <button type="button" data-step="-1" aria-label="Fewer" ${state.unknown === 0 ? 'disabled' : ''}>&minus;</button>
            <output>${state.unknown}</output>
            <button type="button" data-step="1" aria-label="More" ${state.unknown >= f.size ? 'disabled' : ''}>+</button>
          </div>
        </div>`;
    }
    $('#team').innerHTML = `
      <legend>Your team</legend>
      <p class="hint">Every bowler is required. ${f.size} bowlers per team.</p>
      ${stepper}
      ${state.slots.map((_, i) => slotHtml(i)).join('')}`;
    $('#check').disabled = !isComplete();
  }

  function search(i, text) {
    const wrap = document.querySelector(`[data-ac="${i}"]`);
    const panel = wrap.querySelector('.ac-panel');
    const input = wrap.querySelector('input');
    if (text.trim().length < 3) { panel.hidden = true; input.setAttribute('aria-expanded', 'false'); return; }
    panel.hidden = false;
    input.setAttribute('aria-expanded', 'true');
    panel.innerHTML = '<div class="ac-note">Searching...</div>';
    clearTimeout(search.t);
    search.t = setTimeout(() => {
      const taken = chosenIds();
      const q = text.trim().toLowerCase();
      const hits = BOWLERS.filter((b) => !taken.includes(b.id) && b.name.toLowerCase().includes(q)).slice(0, 20);
      if (hits.length === 0) {
        const nf = OPTION === '3' ? `<button type="button" class="link-btn" data-mark="${i}">Mark as not in our system</button>` : '';
        panel.innerHTML = `<div class="ac-note">No bowler found. Check the spelling, or try just a last name.${nf}</div>`;
      } else {
        panel.innerHTML = hits.map((b) => `<div class="ac-option" role="option" data-pick="${b.id}" data-slot="${i}"><span>${esc(b.name)}</span><span class="usbc">${esc(b.usbc)}</span></div>`).join('');
      }
    }, 250);
  }

  document.addEventListener('input', (e) => {
    const wrap = e.target.closest('[data-ac]');
    if (wrap && e.target.matches('input[type="text"]')) search(Number(wrap.dataset.ac), e.target.value);
  });

  document.addEventListener('click', (e) => {
    const t = e.target.closest('button, .ac-option, input[data-unknown]');
    if (!t) { document.querySelectorAll('.ac-panel').forEach((p) => (p.hidden = true)); return; }
    if (t.dataset.pick) {
      state.slots[Number(t.dataset.slot)].bowler = BOWLERS.find((b) => b.id === t.dataset.pick);
      render();
    } else if (t.dataset.clear) { state.slots[Number(t.dataset.clear)].bowler = null; render(); }
    else if (t.dataset.mark) { const s = state.slots[Number(t.dataset.mark)]; s.unknown = true; s.bowler = null; render(); }
    else if (t.dataset.undo) { state.slots[Number(t.dataset.undo)].unknown = false; render(); }
    else if (t.dataset.step) {
      const size = FORMATS[state.format].size;
      state.unknown = Math.max(0, Math.min(size, state.unknown + Number(t.dataset.step)));
      // Slots the stepper now hides give up their bowler.
      state.slots.forEach((s, i) => { if (i >= size - state.unknown) s.bowler = null; });
      render();
    }
  });

  document.addEventListener('change', (e) => {
    if (e.target.matches('input[data-unknown]')) {
      const s = state.slots[Number(e.target.dataset.unknown)];
      s.unknown = e.target.checked; if (s.unknown) s.bowler = null; render();
    }
  });

  $('#check').addEventListener('click', () => { $('#result').innerHTML = RESULTS[state.result]; $('#result').scrollIntoView({ behavior: 'smooth', block: 'nearest' }); });
  $('#clear-all').addEventListener('click', reset);
  $('#mock-format').addEventListener('change', (e) => { state.format = e.target.value; reset(); });
  $('#mock-result').addEventListener('change', (e) => { state.result = e.target.value; if ($('#result').innerHTML) $('#result').innerHTML = RESULTS[state.result]; });

  reset();
})();
