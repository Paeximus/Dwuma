// =============================================
// resume-tailor.js  —  AI Career Agent
// =============================================

const API_BASE = 'http://localhost:5000/api';

// ─── LOADING MESSAGES ────────────────────────────────────────────────────────

const loadingMsgs = [
  'Reading your CV...',
  'Analysing the job description...',
  'Identifying ATS keywords...',
  'Rewriting experience bullets...',
  'Optimising for ATS formatting...',
  'Building your changelog...',
];

let loadingInterval;

function startLoading() {
  let i = 0;
  document.getElementById('loadingText').textContent = loadingMsgs[0];
  loadingInterval = setInterval(() => {
    i = (i + 1) % loadingMsgs.length;
    document.getElementById('loadingText').textContent = loadingMsgs[i];
  }, 2200);
}

function stopLoading() { clearInterval(loadingInterval); }

// ─── CV LOAD HELPERS ─────────────────────────────────────────────────────────

function loadFromProfile() {
  const profile = JSON.parse(localStorage.getItem('userProfile') || '{}');
  if (profile.cvParsedText) {
    document.getElementById('cvText').value = profile.cvParsedText;
  } else {
    // Build a text CV from profile data as fallback
    const lines = [];
    if (profile.firstName) lines.push(`${profile.firstName} ${profile.lastName}`);
    if (profile.email)     lines.push(profile.email);
    if (profile.phone)     lines.push(profile.phone);
    lines.push('');
    if (profile.fieldOfStudy) lines.push(`EDUCATION\n${profile.degreeLevel} in ${profile.fieldOfStudy} — ${profile.institution} (${profile.graduationYear || ''})`);
    if (profile.techSkills?.length) lines.push(`\nSKILLS\n${profile.techSkills.join(', ')}`);
    if (profile.careerGoal) lines.push(`\nPROFILE\n${profile.careerGoal}`);
    document.getElementById('cvText').value = lines.join('\n');
  }
}

function handleCvUpload(event) {
  const file = event.target.files[0];
  if (!file) return;
  if (file.type === 'text/plain') {
    const reader = new FileReader();
    reader.onload = (e) => { document.getElementById('cvText').value = e.target.result; };
    reader.readAsText(file);
  } else {
    // For PDF/DOCX, send to backend for extraction
    uploadCvForParsing(file);
  }
}

async function uploadCvForParsing(file) {
  const fd = new FormData();
  fd.append('file', file);
  try {
    const res = await fetch(`${API_BASE}/resumetailor/parse-cv`, { method: 'POST', body: fd });
    if (res.ok) {
      const { text } = await res.json();
      document.getElementById('cvText').value = text;
    } else {
      showError('Could not parse the uploaded file. Please paste your CV text manually.');
    }
  } catch {
    showError('Upload failed. Please paste your CV text manually.');
  }
}

// ─── MAIN TAILORING ──────────────────────────────────────────────────────────

async function runTailoring() {
  const cvText = document.getElementById('cvText').value.trim();
  const jobDesc = document.getElementById('jobDescription').value.trim();
  const jobTitle = document.getElementById('jobTitle').value.trim();

  if (!cvText)    { showError('Please add your CV content.'); return; }
  if (!jobDesc)   { showError('Please paste the job description.'); return; }
  if (!jobTitle)  { showError('Please enter the job title.'); return; }

  hideError();

  document.getElementById('inputPanel').style.display = 'none';
  document.getElementById('loadingPanel').style.display = 'block';
  startLoading();

  const payload = {
    cvText,
    jobTitle,
    companyName:    document.getElementById('companyName').value.trim(),
    jobDescription: jobDesc,
    tailoringFocus: document.getElementById('tailoringFocus').value,
  };

  try {
    const res = await fetch(`${API_BASE}/resumetailor/tailor`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload),
    });

    if (!res.ok) {
      const err = await res.json().catch(() => ({}));
      throw new Error(err.message || `HTTP ${res.status}`);
    }

    const data = await res.json();
    stopLoading();
    document.getElementById('loadingPanel').style.display = 'none';
    renderResults(data, payload);

  } catch (err) {
    stopLoading();
    document.getElementById('loadingPanel').style.display = 'none';
    document.getElementById('inputPanel').style.display = 'block';
    showError('Tailoring failed: ' + (err.message || 'Please try again.'));
  }
}

// ─── RENDER RESULTS ───────────────────────────────────────────────────────────

let currentResult = null;

function renderResults(data, payload) {
  currentResult = { data, payload };

  document.getElementById('resultsPanel').style.display = 'block';

  // Header
  document.getElementById('resultTitle').textContent =
    payload.companyName
      ? `${payload.jobTitle} at ${payload.companyName}`
      : payload.jobTitle;
  document.getElementById('resultSubtitle').textContent =
    `Tailored ${new Date().toLocaleDateString('en-GB', { day:'numeric', month:'short', year:'numeric' })}`;

  // ATS score ring
  renderAtsScore(data.atsScore ?? 0, data.atsSummary ?? '', data.matchedKeywords ?? [], data.missingKeywords ?? []);

  // Diff
  document.getElementById('originalPane').textContent = payload.cvText;
  document.getElementById('tailoredPane').textContent = data.tailoredCv ?? '';

  // Changelog
  renderChangelog(data.changelog ?? []);

  // Load saved versions from localStorage
  renderVersionList();
}

function renderAtsScore(score, summary, matched, missing) {
  document.getElementById('atsNum').textContent = score;

  // Animate ring
  const circumference = 226;
  const offset = circumference - (score / 100) * circumference;
  setTimeout(() => {
    document.getElementById('atsArc').style.strokeDashoffset = offset;
  }, 100);

  // Colour by score
  const color = score >= 75 ? 'var(--accent)' : score >= 50 ? 'var(--warn)' : 'var(--danger)';
  document.getElementById('atsArc').style.stroke = color;
  document.getElementById('atsNum').style.color  = color;

  document.getElementById('atsTitle').textContent =
    score >= 75 ? 'Strong ATS match' : score >= 50 ? 'Moderate match' : 'Needs improvement';
  document.getElementById('atsDesc').textContent = summary;

  // Keywords
  const chips = [
    ...matched.map(k => `<span class="keyword-chip keyword-matched">✓ ${esc(k)}</span>`),
    ...missing.map(k => `<span class="keyword-chip keyword-missing">✗ ${esc(k)}</span>`),
  ];
  document.getElementById('keywordList').innerHTML = chips.join('');
}

function renderChangelog(items) {
  if (!items.length) {
    document.getElementById('changelogList').innerHTML =
      '<p style="color:var(--text-muted);font-size:0.875rem;">No changes logged.</p>';
    return;
  }

  const typeMap = {
    rewrite: 'change-rewrite',
    added:   'change-added',
    removed: 'change-removed',
    keyword: 'change-keyword',
  };

  document.getElementById('changelogList').innerHTML = items.map(c => `
    <div class="changelog-item">
      <span class="change-type ${typeMap[c.type] ?? 'change-rewrite'}">${esc(c.type ?? 'edit')}</span>
      <div>
        <div class="change-section">${esc(c.section ?? '')}</div>
        <div class="change-reason">${esc(c.reason ?? '')}</div>
      </div>
    </div>`).join('');
}

// ─── VERSION HISTORY (localStorage) ──────────────────────────────────────────

function saveVersion() {
  if (!currentResult) return;
  const versions = JSON.parse(localStorage.getItem('cvVersions') || '[]');
  versions.unshift({
    id:          Date.now(),
    jobTitle:    currentResult.payload.jobTitle,
    companyName: currentResult.payload.companyName,
    date:        new Date().toISOString(),
    tailoredCv:  currentResult.data.tailoredCv,
    atsScore:    currentResult.data.atsScore,
  });
  localStorage.setItem('cvVersions', JSON.stringify(versions.slice(0, 10)));
  renderVersionList();
}

function renderVersionList() {
  const versions = JSON.parse(localStorage.getItem('cvVersions') || '[]');
  if (!versions.length) return;

  document.getElementById('versionList').innerHTML = versions.map(v => `
    <div class="version-item">
      <div class="version-dot"></div>
      <div class="version-info">
        <div class="version-role">${esc(v.jobTitle)}${v.companyName ? ` — ${esc(v.companyName)}` : ''}</div>
        <div class="version-date">${new Date(v.date).toLocaleDateString('en-GB', {day:'numeric',month:'short',year:'numeric'})} · ATS ${v.atsScore ?? '?'}%</div>
      </div>
      <button class="btn btn-secondary" style="padding:5px 12px;font-size:0.78rem;" onclick="loadVersion(${v.id})">Load</button>
    </div>`).join('');
}

function loadVersion(id) {
  const versions = JSON.parse(localStorage.getItem('cvVersions') || '[]');
  const v = versions.find(x => x.id === id);
  if (!v) return;
  document.getElementById('tailoredPane').textContent = v.tailoredCv ?? '';
}

// ─── DOWNLOAD DOCX ───────────────────────────────────────────────────────────

async function downloadCv() {
  if (!currentResult) return;

  const btn = document.getElementById('downloadBtn');
  btn.disabled = true;
  btn.textContent = 'Generating...';

  try {
    const res = await fetch(`${API_BASE}/resumetailor/download`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        tailoredCv:  currentResult.data.tailoredCv,
        jobTitle:    currentResult.payload.jobTitle,
        companyName: currentResult.payload.companyName,
      }),
    });

    if (!res.ok) throw new Error('Download failed.');

    const blob = await res.blob();
    const url  = URL.createObjectURL(blob);
    const a    = document.createElement('a');
    a.href     = url;
    a.download = `CV_${(currentResult.payload.jobTitle || 'Tailored').replace(/\s+/g, '_')}.docx`;
    a.click();
    URL.revokeObjectURL(url);

  } catch (err) {
    showError('Download failed: ' + err.message);
  } finally {
    btn.disabled = false;
    btn.textContent = '⬇ Download DOCX';
  }
}

// ─── HELPERS ─────────────────────────────────────────────────────────────────

function resetTailor() {
  document.getElementById('resultsPanel').style.display = 'none';
  document.getElementById('inputPanel').style.display  = 'block';
  currentResult = null;
}

function clearAll() {
  ['cvText','jobTitle','companyName','jobDescription'].forEach(id => {
    document.getElementById(id).value = '';
  });
  hideError();
}

function showError(msg) {
  const el = document.getElementById('inputError');
  el.textContent = msg;
  el.style.display = 'block';
}

function hideError() {
  document.getElementById('inputError').style.display = 'none';
}

function esc(s) {
  return String(s)
    .replace(/&/g,'&amp;').replace(/</g,'&lt;')
    .replace(/>/g,'&gt;').replace(/"/g,'&quot;');
}
