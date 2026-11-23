// Shared rendering and interaction for the Series advancement mockups. Throwaway review code: it simulates
// what the Blazor components do (status pills, output panels, override modal, squad picker, recompute confirm).
(function () {
  const A = window.ADV_DATA;
  const S = { key: 'mid', data: null, expanded: new Set(), selected: 1, loading: false, render() {} };

  const esc = s => String(s ?? '').replace(/[&<>"]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[c]));
  const ord = n => { const s = ['th', 'st', 'nd', 'rd'], v = n % 100; return n + (s[(v - 20) % 10] || s[v] || s[0]); };
  const plural = (n, one, many) => `${n} ${n === 1 ? one : many}`;
  function wordList(nums) {
    if (nums.length === 1) return String(nums[0]);
    return `${nums.slice(0, -1).join(', ')} and ${nums[nums.length - 1]}`;
  }
  const lastQualifying = () => S.data.stages.filter(s => s.kind === 'Qualifying').slice(-1)[0];
  const stage = order => S.data.stages[order - 1];
  const today = '11/20/2026';

  /* ---------- Display pieces ---------- */
  const pill = st => `<span class="pill pill--${st.status.toLowerCase()}">${st.status}</span>`;

  function flags(st) {
    const out = [];
    if (st.tournamentStatus === 'Cancelled') {
      out.push('<span class="flag flag--error">Tournament cancelled: <a href="../series-management/series-editor.html">remove this Stage</a></span>');
    }
    if (st.needsSquad) out.push('<span class="flag flag--warn">Needs squad</span>');
    if (st.isStale) out.push('<span class="flag flag--warn" title="An earlier Stage changed after this Stage was official.">Stale</span>');
    if (st.status === 'Computed' && st.officialDueAt) out.push(`<span class="flag flag--info">Becomes official about ${esc(st.officialDueAt)}</span>`);
    if (st === lastQualifying() && S.data.backfillAsOf) out.push(`<span class="flag">Backfill as of ${esc(S.data.backfillAsOf)}</span>`);
    return out.length ? `<div class="flags">${out.join('')}</div>` : '';
  }

  function actions(st) {
    const a = [];
    if (st.kind === 'Survivor') a.push(`<button type="button" class="act" data-act="pick" data-stage="${st.order}">Pick squad</button>`);
    a.push(`<button type="button" class="act" data-act="recompute" data-stage="${st.order}">Recompute from here</button>`);
    return `<div class="acts">${a.join('')}</div>`;
  }

  function expander(st) {
    const open = S.expanded.has(st.order);
    return `<button type="button" class="expander" data-act="expand" data-stage="${st.order}" aria-expanded="${open}" aria-controls="out-${st.order}" aria-label="${open ? 'Hide' : 'Show'} output for Stage ${st.order}">
      <svg width="12" height="12" viewBox="0 0 12 12" aria-hidden="true"><path d="M4 2l4 4-4 4" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"/></svg></button>`;
  }

  function pendingReason(st) {
    if (st.tournamentStatus === 'Cancelled') return 'This tournament was cancelled. Remove this Stage on the Edit page, then recompute from the Stage before it.';
    if (st.needsSquad) return 'Waiting for a squad. Pick the squad this round uses and the Stage computes.';
    if (st.kind === 'Placement') return 'Positions appear when the Placement Round computes.';
    return 'Not computed yet. This Stage computes when its tournament completes.';
  }

  function qualifyingOutput(st) {
    const summary = `<p class="output-summary"><strong>${plural(st.slotCount, 'slot', 'slots')}</strong>${st.vacant ? `, ${plural(st.vacant, 'vacant slot', 'vacant slots')}. Vacant slots go to the Backfill.` : ', none vacant.'}</p>`;
    const canEdit = st.status === 'Computed';
    const rows = st.entries.map(e => `
      <tr>
        <td data-label="Place" class="num">${e.place ? ord(e.place) : '&mdash;'}</td>
        <td data-label="Bowler">${esc(e.bowler)}</td>
        <td data-label="Source" class="src ${e.source === 'Admin' ? 'src--admin' : e.source === 'Backfill' ? 'src--backfill' : ''}">${e.source === 'Admin' ? 'Added by admin' : e.source}</td>
        <td data-label="Backfill points" class="num">${e.points ?? '&mdash;'}</td>
        <td data-label="Actions" class="act-cell admin-only">${canEdit && e.source === 'Qualifier'
          ? `<button type="button" class="act" data-act="clear" data-stage="${st.order}" data-bowler="${esc(e.bowler)}" data-place="${e.place}">Clear slot</button>` : ''}</td>
      </tr>`).join('');
    const overrides = st.overrides.length ? `
      <h4>Admin overrides</h4>
      <div class="scroll scroll--free"><table class="data-table cols-5">
        <thead><tr><th>Change</th><th>Bowler</th><th>Reason</th><th>Added</th><th>Status</th><th class="admin-only">Actions</th></tr></thead>
        <tbody>${st.overrides.map(o => `
          <tr>
            <td data-label="Change">${o.kind === 'Add' ? 'Add bowler' : 'Clear slot'}</td>
            <td data-label="Bowler">${esc(o.bowler)}</td>
            <td data-label="Reason" class="reason">${esc(o.reason)}</td>
            <td data-label="Added">${o.date}</td>
            <td data-label="Status">${o.applied ? 'Applied' : '<span class="inactive">No longer applies</span>'}</td>
            <td data-label="Actions" class="act-cell admin-only">${canEdit ? `<button type="button" class="act" data-act="remove" data-stage="${st.order}" data-bowler="${esc(o.bowler)}">Remove override</button>` : ''}</td>
          </tr>`).join('')}</tbody></table></div>` : '';
    return `${summary}
      <div class="output-tools">
        <span class="muted" style="font-size:14px">Field entries from this Stage${st === lastQualifying() ? ', then the Backfill' : ''}</span>
        ${canEdit ? `<button type="button" class="btn btn-secondary admin-only" data-act="add" data-stage="${st.order}">Add bowler</button>` : ''}
      </div>
      ${st.entries.length ? `<div class="scroll scroll--free"><table class="data-table cols-5">
        <thead><tr><th>Place</th><th>Bowler</th><th>Source</th><th>Backfill points</th><th class="admin-only">Actions</th></tr></thead>
        <tbody>${rows}</tbody></table></div>` : '<p class="output-empty">No bowlers in the field from this Stage.</p>'}
      ${overrides}`;
  }

  function survivorOutput(st) {
    const bowled = st.outcomes.filter(o => o.block !== null).length;
    const adv = st.outcomes.filter(o => o.advanced).length;
    return `<p class="output-summary"><strong>${adv} advance</strong> of ${st.outcomes.length} in the field. ${bowled} bowled; ${st.outcomes.length - bowled} did not.</p>
      <div class="scroll"><table class="data-table">
        <thead><tr><th>Bowler</th><th>Block total</th><th>Advanced</th></tr></thead>
        <tbody>${st.outcomes.map(o => `
          <tr class="${o.advanced ? '' : 'is-out'}">
            <td data-label="Bowler">${esc(o.bowler)}</td>
            <td data-label="Block total" class="num">${o.block ?? 'No full block'}</td>
            <td data-label="Advanced">${o.advanced ? 'Yes' : 'No'}</td>
          </tr>`).join('')}</tbody></table></div>`;
  }

  function placementOutput(st) {
    const fin = S.data.hasFinale;
    const p = S.data.positions;
    return `<p class="output-summary"><strong>${plural(p.length, 'bowler', 'bowlers')} ranked.</strong> ${fin ? 'Finalist positions are provisional until the Finale is recorded.' : 'These positions are final.'}</p>
      <div class="scroll"><table class="data-table${fin ? ' cols-4' : ''}">
        <thead><tr><th>Position</th><th>Bowler</th><th>Bowled</th>${fin ? '<th>Finalist</th>' : ''}</tr></thead>
        <tbody>${p.map(r => `
          <tr>
            <td data-label="Position" class="num">${ord(r.position)}</td>
            <td data-label="Bowler">${esc(r.bowler)}</td>
            <td data-label="Bowled">${r.bowled ? 'Yes' : 'No'}</td>
            ${fin ? `<td data-label="Finalist">${r.finalist ? 'Yes' : 'No'}</td>` : ''}
          </tr>`).join('')}</tbody></table></div>`;
  }

  function output(st) {
    const stale = st.isStale ? '<p class="output-summary" style="color:var(--warn-fg)">An earlier Stage changed after this Stage was official. Its output has not changed. Recompute from here to refresh it.</p>' : '';
    if (st.status === 'Pending') return `<div class="output" id="out-${st.order}">${flags(st) ? '' : ''}<p class="output-empty">${pendingReason(st)}</p></div>`;
    const body = st.kind === 'Qualifying' ? qualifyingOutput(st) : st.kind === 'Survivor' ? survivorOutput(st) : placementOutput(st);
    return `<div class="output" id="out-${st.order}">${stale}${body}</div>`;
  }

  const skeleton = (n = 4) => Array.from({ length: n }, () => '<div class="skeleton" style="margin-bottom:10px"></div>').join('');

  function progress() {
    const st = S.data.stages;
    const done = st.filter(s => s.status !== 'Pending').length;
    const ann = st.filter(s => s.status === 'Official').length;
    const bf = S.data.backfillAsOf ? `Backfill ran as of ${S.data.backfillAsOf}.` : 'Backfill has not run.';
    return `<strong>${done} of ${st.length} Stages</strong> computed or official (${ann} official). ${bf} Version ${S.data.version}.`;
  }

  /* ---------- Modals ---------- */
  const modals = [];
  function modalRoot() {
    let r = document.getElementById('modal-root');
    if (!r) { r = document.createElement('div'); r.id = 'modal-root'; document.body.appendChild(r); }
    return r;
  }
  function toast(msg, warn) {
    let t = document.getElementById('toasts');
    if (!t) { t = document.createElement('div'); t.id = 'toasts'; t.className = 'toasts'; t.setAttribute('role', 'status'); document.body.appendChild(t); }
    const el = document.createElement('div');
    el.className = 'toast' + (warn ? ' toast--warn' : '');
    el.textContent = msg;
    t.appendChild(el);
    setTimeout(() => el.remove(), 4200);
  }
  function showModal(id, html, { narrow = false, requestClose } = {}) {
    const el = document.createElement('div');
    el.className = 'modal-backdrop open';
    el.dataset.modal = id;
    el.innerHTML = `<div class="panel modal${narrow ? ' modal--narrow' : ''}" role="dialog" aria-modal="true" aria-labelledby="${id}-title">${html}</div>`;
    modalRoot().appendChild(el);
    const entry = { id, el, requestClose: requestClose || (() => closeModal(id)) };
    modals.push(entry);
    el.addEventListener('mousedown', e => { if (e.target === el) entry.requestClose(); });
    const first = el.querySelector('input,textarea,button.btn-primary,button');
    if (first) setTimeout(() => first.focus(), 0);
    return el;
  }
  function closeModal(id) {
    const i = modals.findIndex(m => m.id === id);
    if (i >= 0) { modals[i].el.remove(); modals.splice(i, 1); }
  }
  document.addEventListener('keydown', e => {
    if (e.key === 'Escape' && modals.length && !e.defaultPrevented) modals[modals.length - 1].requestClose();
  });

  function confirmModal({ id = 'confirm', title, message, confirmLabel, busyLabel, danger, cancelLabel = 'Cancel', onConfirm, onCancel }) {
    const el = showModal(id, `
      <h2 id="${id}-title">${esc(title)}</h2>
      <p class="lead" style="margin-bottom:0">${esc(message)}</p>
      <div class="modal-actions">
        <button type="button" class="btn btn-secondary" data-x="cancel">${esc(cancelLabel)}</button>
        <button type="button" class="btn ${danger ? 'btn-danger' : 'btn-primary'}" data-x="ok">${esc(confirmLabel)}</button>
      </div>`, { narrow: true, requestClose: () => { closeModal(id); onCancel && onCancel(); } });
    el.querySelector('[data-x=cancel]').addEventListener('click', () => { closeModal(id); onCancel && onCancel(); });
    el.querySelector('[data-x=ok]').addEventListener('click', e => {
      e.target.disabled = true; e.target.textContent = busyLabel || 'Working...';
      setTimeout(() => { closeModal(id); onConfirm(); }, 450);
    });
  }

  /* Recompute from here: runs directly unless official Stages in range would reset. */
  function recompute(order) {
    const official = S.data.stages.filter(s => s.order >= order && s.status === 'Official').map(s => s.order);
    const run = () => {
      S.data.stages.forEach(s => { if (s.order >= order && s.status === 'Official') { s.status = 'Computed'; s.officialDueAt = '4:30 PM on 11/21'; } if (s.order >= order) s.isStale = false; });
      S.data.version++;
      toast(`Recomputed from Stage ${order}.`);
      S.render();
    };
    if (!official.length) { run(); return; }
    confirmModal({
      id: 'recompute', title: `Recompute from Stage ${order}?`,
      message: `${official.length === 1 ? 'Stage' : 'Stages'} ${wordList(official)} ${official.length === 1 ? 'is' : 'are'} official. Recomputing resets ${official.length === 1 ? 'it' : 'them'} to Computed.`,
      confirmLabel: 'Recompute', busyLabel: 'Recomputing...', danger: true, onConfirm: run,
    });
  }

  /* Pick squad */
  function pickSquad(order) {
    const st = stage(order);
    const opts = st.squadOptions;
    const body = opts.length ? `<fieldset class="radio-list" aria-label="Squads">${opts.map(o => `
        <label class="radio-row"><input type="radio" name="squad" value="${o.id}" ${o.current ? 'checked' : ''}>
          <span><b>${o.label}</b><small>${o.hasScores ? 'Has scores' : 'No scores yet'}${o.current ? ' &middot; Current' : ''}</small></span></label>`).join('')}</fieldset>`
      : '<p class="output-empty">No squads yet. They arrive when the desktop software sends this tournament&rsquo;s squads.</p>';
    const el = showModal('squad', `
      <h2 id="squad-title">Pick squad</h2>
      <p class="lead">Stage ${order}, ${esc(st.tournament)}. The Series expects ${esc(st.squadDescription || 'a squad')}. No squad matches it exactly, so pick the one this round uses.</p>
      ${body}
      <div class="modal-actions">
        <button type="button" class="btn btn-secondary" data-x="cancel">Cancel</button>
        <button type="button" class="btn btn-primary" data-x="save" disabled>Use this squad</button>
      </div>`);
    const save = el.querySelector('[data-x=save]');
    const current = (opts.find(o => o.current) || {}).id;
    el.querySelectorAll('input[name=squad]').forEach(r => r.addEventListener('change', () => { save.disabled = r.value === current; }));
    el.querySelector('[data-x=cancel]').addEventListener('click', () => closeModal('squad'));
    save.addEventListener('click', () => {
      save.disabled = true; save.textContent = 'Saving...';
      setTimeout(() => {
        const chosen = el.querySelector('input[name=squad]:checked').value;
        st.squadResolved = chosen; st.needsSquad = false;
        A.computeSurvivor(S.data, order, false);
        S.data.version++;
        closeModal('squad');
        toast(`Squad set. Stage ${order} computed.`);
        S.render();
      }, 500);
    });
  }

  /* Add bowler / Clear slot */
  function overrideModal({ kind, order, bowler, place }) {
    const st = stage(order);
    const rows = kind === 'Add' ? A.resultRows(st) : [];
    const state = { bowler: kind === 'Clear' ? bowler : null, place: kind === 'Clear' ? Number(place) : null, reason: '', active: -1 };
    const title = kind === 'Add' ? 'Add bowler' : 'Clear slot';
    const lead = kind === 'Add'
      ? `Stage ${order}, ${esc(st.tournament)}. Pick any bowler with a result in this tournament, whatever the place or membership. The field can then be larger than the target.`
      : `Stage ${order}, ${esc(st.tournament)}. The slot goes vacant and the Backfill fills it. This bowler can't come back through the Backfill.`;
    const pick = kind === 'Add' ? `
      <div class="field"><label for="ov-bowler">Bowler</label>
        <div class="ac"><input class="input" id="ov-bowler" role="combobox" aria-expanded="false" aria-controls="ov-list" autocomplete="off" placeholder="Search by name">
          <ul class="ac-list" id="ov-list" role="listbox" hidden></ul></div></div>`
      : `<div class="field"><span class="lbl">Bowler</span><div class="readonly-box"><strong>${esc(bowler)}</strong> &middot; ${ord(state.place)} place</div></div>`;
    const el = showModal('override', `
      <h2 id="override-title">${title}</h2>
      <p class="lead">${lead}</p>
      <div id="ov-error"></div>
      ${pick}
      <div class="field"><label for="ov-reason">Reason <span class="req">(required)</span></label>
        <textarea class="textarea" id="ov-reason" maxlength="600" aria-describedby="ov-count"></textarea>
        <div class="hint"><span>Shown to other admins. Say who confirmed it.</span><span id="ov-count">0 / 500</span></div></div>
      <div class="modal-actions">
        <button type="button" class="btn btn-secondary" data-x="cancel">Cancel</button>
        <button type="button" class="btn btn-primary" data-x="save" disabled>${title}</button>
      </div>`, { requestClose: attemptClose });

    const save = el.querySelector('[data-x=save]');
    const reason = el.querySelector('#ov-reason');
    const count = el.querySelector('#ov-count');
    const isDirty = () => !!state.reason || (kind === 'Add' && !!state.bowler);
    function validate() {
      const len = state.reason.trim().length;
      count.textContent = `${state.reason.length} / 500`;
      count.className = state.reason.length > 500 ? 'over' : '';
      save.disabled = !(state.bowler && len > 0 && state.reason.length <= 500);
    }
    function attemptClose() {
      if (!isDirty()) { closeModal('override'); return; }
      confirmModal({ id: 'discard', title: 'Discard changes?', message: 'You have not saved this change. Closing now loses what you typed.', confirmLabel: 'Discard', cancelLabel: 'Keep editing', danger: true, onConfirm: () => closeModal('override') });
    }
    reason.addEventListener('input', () => { state.reason = reason.value; validate(); });
    el.querySelector('[data-x=cancel]').addEventListener('click', attemptClose);

    if (kind === 'Add') {
      const input = el.querySelector('#ov-bowler');
      const list = el.querySelector('#ov-list');
      let shown = [];
      const draw = () => {
        const q = input.value.trim().toLowerCase();
        shown = rows.filter(r => !q || r.bowler.toLowerCase().includes(q));
        list.hidden = false; input.setAttribute('aria-expanded', 'true');
        list.innerHTML = shown.length ? shown.map((r, i) => `<li role="option" data-i="${i}" aria-selected="${i === state.active}">${esc(r.bowler)}<small>${ord(r.place)}</small></li>`).join('')
          : '<div class="ac-empty">No bowler with a result matches. Bowlers already in the field are not listed.</div>';
      };
      const choose = r => { state.bowler = r.bowler; state.place = r.place; input.value = `${r.bowler} (${ord(r.place)})`; list.hidden = true; input.setAttribute('aria-expanded', 'false'); validate(); };
      input.addEventListener('focus', draw);
      input.addEventListener('input', () => { state.bowler = null; state.active = -1; draw(); validate(); });
      input.addEventListener('keydown', e => {
        if (e.key === 'ArrowDown') { state.active = Math.min(shown.length - 1, state.active + 1); draw(); e.preventDefault(); }
        else if (e.key === 'ArrowUp') { state.active = Math.max(0, state.active - 1); draw(); e.preventDefault(); }
        else if (e.key === 'Enter' && shown[state.active]) { choose(shown[state.active]); e.preventDefault(); }
        else if (e.key === 'Escape' && !list.hidden) { list.hidden = true; e.preventDefault(); e.stopPropagation(); }
      });
      list.addEventListener('mousedown', e => { const li = e.target.closest('li'); if (li) { e.preventDefault(); choose(shown[Number(li.dataset.i)]); } });
      input.addEventListener('blur', () => setTimeout(() => { list.hidden = true; }, 120));
    }

    save.addEventListener('click', () => {
      const fail = document.getElementById('toggle-fail');
      save.disabled = true; save.textContent = 'Saving...';
      setTimeout(() => {
        if (fail && fail.checked) {
          fail.checked = false;
          el.querySelector('#ov-error').innerHTML = `<div class="alert alert--error" role="alert">${kind === 'Add' ? `${esc(state.bowler)} is already in the Series Field.` : `${esc(state.bowler)} does not hold a slot in this Stage.`} Pick someone else, or close and review the Stage.</div>`;
          save.textContent = title; validate();
          return;
        }
        if (kind === 'Add') {
          st.entries.push({ bowler: state.bowler, place: state.place, source: 'Admin', points: null });
          st.overrides.push({ kind: 'Add', bowler: state.bowler, reason: state.reason.trim(), date: today, applied: true });
          toast(`${state.bowler} added to Stage ${order}.`);
        } else {
          st.entries = st.entries.filter(e => e.bowler !== state.bowler);
          st.vacant++;
          st.overrides.push({ kind: 'Clear', bowler: state.bowler, reason: state.reason.trim(), date: today, applied: true, place: state.place });
          toast('Slot cleared. The Backfill will refill it.');
        }
        S.data.version++;
        closeModal('override');
        S.render();
      }, 500);
    });
  }

  function removeOverride(order, bowler) {
    const st = stage(order);
    const ov = st.overrides.find(o => o.bowler === bowler);
    if (!ov) return;
    confirmModal({
      id: 'remove', title: 'Remove this override?',
      message: ov.kind === 'Add' ? `${bowler} leaves the field for Stage ${order}.` : `${bowler} takes the slot back, and the Backfill no longer refills it.`,
      confirmLabel: 'Remove override', busyLabel: 'Removing...', danger: true,
      onConfirm: () => {
        st.overrides = st.overrides.filter(o => o !== ov);
        if (ov.kind === 'Add') st.entries = st.entries.filter(e => e.bowler !== bowler);
        else { st.entries.push({ bowler, place: ov.place, source: 'Qualifier', points: null }); st.entries.sort((a, b) => (a.place ?? 99) - (b.place ?? 99)); st.vacant = Math.max(0, st.vacant - 1); }
        S.data.version++;
        toast('Override removed.');
        S.render();
      },
    });
  }

  /* ---------- Wiring ---------- */
  document.addEventListener('click', e => {
    const tr = e.target.closest('.data-table tr');
    if (tr && !e.target.closest('button,a')) tr.classList.toggle('open');
    const el = e.target.closest('[data-act]');
    if (!el) return;
    const order = Number(el.dataset.stage);
    switch (el.dataset.act) {
      case 'expand': S.expanded.has(order) ? S.expanded.delete(order) : S.expanded.add(order); S.render(); break;
      case 'select': S.selected = order; S.render(); break;
      case 'recompute': recompute(order); break;
      case 'pick': pickSquad(order); break;
      case 'add': overrideModal({ kind: 'Add', order }); break;
      case 'clear': overrideModal({ kind: 'Clear', order, bowler: el.dataset.bowler, place: el.dataset.place }); break;
      case 'remove': removeOverride(order, el.dataset.bowler); break;
    }
  });

  function init({ render, scenario = 'mid' }) {
    S.render = () => { if (S.loading) { render(); return; } render(); };
    const sc = document.getElementById('scenario');
    const role = document.getElementById('role');
    const loading = document.getElementById('toggle-loading');
    const load = key => { S.key = key; S.data = A.build(key); S.expanded = new Set(); S.render(); };
    if (sc) { sc.value = scenario; sc.addEventListener('change', () => load(sc.value)); }
    if (role) role.addEventListener('change', () => { document.body.classList.toggle('is-admin', role.value === 'admin'); });
    if (loading) loading.addEventListener('change', () => { S.loading = loading.checked; S.render(); });
    document.body.classList.toggle('is-admin', !role || role.value === 'admin');
    load(scenario);
  }

  window.ADV_UI = { state: S, init, pill, flags, actions, expander, output, progress, skeleton, esc, ord, plural, overrideModal, pickSquad, recompute, lastQualifying };
})();
