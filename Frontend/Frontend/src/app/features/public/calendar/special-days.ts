export interface SpecialDay {
  month: number; // 1-based
  day: number;
  label: string;
  type: 'holiday' | 'awareness' | 'cultural';
}

// ─── Fixed-date awareness & UN observance days ───────────────────────────────
const FIXED_AWARENESS: SpecialDay[] = [
  // January
  { month:1,  day:1,  label:'New Year\'s Day',               type:'holiday'   },
  { month:1,  day:4,  label:'World Braille Day',              type:'awareness' },
  { month:1,  day:24, label:'Int\'l Education Day',           type:'awareness' },
  { month:1,  day:27, label:'Holocaust Remembrance Day',      type:'awareness' },
  // February
  { month:2,  day:2,  label:'World Wetlands Day',             type:'awareness' },
  { month:2,  day:4,  label:'World Cancer Day',               type:'awareness' },
  { month:2,  day:6,  label:'Int\'l Day of Zero Tolerance to FGM', type:'awareness' },
  { month:2,  day:11, label:'Women in Science Day',           type:'awareness' },
  { month:2,  day:13, label:'World Radio Day',                type:'awareness' },
  { month:2,  day:14, label:'Valentine\'s Day',               type:'cultural'  },
  { month:2,  day:20, label:'World Day of Social Justice',    type:'awareness' },
  { month:2,  day:21, label:'Int\'l Mother Language Day',     type:'awareness' },
  // March
  { month:3,  day:1,  label:'Zero Discrimination Day',        type:'awareness' },
  { month:3,  day:3,  label:'World Wildlife Day',             type:'awareness' },
  { month:3,  day:8,  label:'Int\'l Women\'s Day',            type:'awareness' },
  { month:3,  day:15, label:'World Consumer Rights Day',      type:'awareness' },
  { month:3,  day:17, label:'St. Patrick\'s Day',             type:'cultural'  },
  { month:3,  day:20, label:'Int\'l Day of Happiness',        type:'awareness' },
  { month:3,  day:21, label:'World Down Syndrome Day',        type:'awareness' },
  { month:3,  day:21, label:'Int\'l Day for Elimination of Racial Discrimination', type:'awareness' },
  { month:3,  day:22, label:'World Water Day',                type:'awareness' },
  { month:3,  day:23, label:'World Meteorological Day',       type:'awareness' },
  { month:3,  day:24, label:'World Tuberculosis Day',         type:'awareness' },
  { month:3,  day:25, label:'Int\'l Day of Remembrance of Slavery', type:'awareness' },
  // April
  { month:4,  day:2,  label:'World Autism Awareness Day',     type:'awareness' },
  { month:4,  day:4,  label:'Int\'l Day for Mine Awareness',  type:'awareness' },
  { month:4,  day:7,  label:'World Health Day',               type:'awareness' },
  { month:4,  day:22, label:'Earth Day',                      type:'awareness' },
  { month:4,  day:23, label:'World Book & Copyright Day',     type:'awareness' },
  { month:4,  day:25, label:'World Malaria Day',              type:'awareness' },
  { month:4,  day:26, label:'World Intellectual Property Day',type:'awareness' },
  { month:4,  day:28, label:'World Day for Safety at Work',   type:'awareness' },
  { month:4,  day:30, label:'Int\'l Jazz Day',                type:'awareness' },
  // May
  { month:5,  day:1,  label:'International Labour Day',       type:'holiday'   },
  { month:5,  day:3,  label:'World Press Freedom Day',        type:'awareness' },
  { month:5,  day:4,  label:'Star Wars Day',                  type:'cultural'  },
  { month:5,  day:8,  label:'World Red Cross Day',            type:'awareness' },
  { month:5,  day:10, label:'World Lupus Day',                type:'awareness' },
  { month:5,  day:15, label:'Int\'l Day of Families',         type:'awareness' },
  { month:5,  day:17, label:'World Telecommunication Day',    type:'awareness' },
  { month:5,  day:18, label:'Int\'l Museum Day',              type:'awareness' },
  { month:5,  day:20, label:'World Bee Day',                  type:'awareness' },
  { month:5,  day:21, label:'World Cultural Diversity Day',   type:'awareness' },
  { month:5,  day:22, label:'Int\'l Day for Biological Diversity', type:'awareness' },
  { month:5,  day:25, label:'Africa Day',                     type:'awareness' },
  { month:5,  day:31, label:'World No Tobacco Day',           type:'awareness' },
  // June
  { month:6,  day:1,  label:'Global Day of Parents',          type:'awareness' },
  { month:6,  day:4,  label:'Int\'l Day of Innocent Children Victims', type:'awareness' },
  { month:6,  day:5,  label:'World Environment Day',          type:'awareness' },
  { month:6,  day:7,  label:'World Food Safety Day',          type:'awareness' },
  { month:6,  day:8,  label:'World Oceans Day',               type:'awareness' },
  { month:6,  day:12, label:'World Day Against Child Labour', type:'awareness' },
  { month:6,  day:13, label:'Int\'l Albinism Awareness Day',  type:'awareness' },
  { month:6,  day:14, label:'World Blood Donor Day',          type:'awareness' },
  { month:6,  day:15, label:'World Elder Abuse Awareness Day',type:'awareness' },
  { month:6,  day:16, label:'Int\'l Day of African Child',    type:'awareness' },
  { month:6,  day:17, label:'World Day to Combat Desertification', type:'awareness' },
  { month:6,  day:18, label:'Sustainable Gastronomy Day',     type:'awareness' },
  { month:6,  day:19, label:'Int\'l Day for Elimination of Sexual Violence', type:'awareness' },
  { month:6,  day:20, label:'World Refugee Day',              type:'awareness' },
  { month:6,  day:21, label:'World Music Day',                type:'awareness' },
  { month:6,  day:21, label:'Int\'l Yoga Day',                type:'awareness' },
  { month:6,  day:23, label:'Int\'l Widows Day',              type:'awareness' },
  { month:6,  day:25, label:'Day of the Seafarer',            type:'awareness' },
  { month:6,  day:26, label:'Int\'l Day Against Drug Abuse',  type:'awareness' },
  // July
  { month:7,  day:1,  label:'Int\'l Joke Day',                type:'cultural'  },
  { month:7,  day:4,  label:'US Independence Day',            type:'holiday'   },
  { month:7,  day:11, label:'World Population Day',           type:'awareness' },
  { month:7,  day:15, label:'World Youth Skills Day',         type:'awareness' },
  { month:7,  day:18, label:'Nelson Mandela Int\'l Day',      type:'awareness' },
  { month:7,  day:28, label:'World Hepatitis Day',            type:'awareness' },
  { month:7,  day:30, label:'Int\'l Day of Friendship',       type:'awareness' },
  // August
  { month:8,  day:9,  label:'Int\'l Day of World\'s Indigenous Peoples', type:'awareness' },
  { month:8,  day:12, label:'Int\'l Youth Day',               type:'awareness' },
  { month:8,  day:15, label:'World Humanitarian Day',         type:'awareness' },
  { month:8,  day:19, label:'World Photography Day',          type:'awareness' },
  { month:8,  day:23, label:'Int\'l Day for Remembrance of Slave Trade', type:'awareness' },
  // September
  { month:9,  day:5,  label:'Int\'l Day of Charity',          type:'awareness' },
  { month:9,  day:8,  label:'Int\'l Literacy Day',            type:'awareness' },
  { month:9,  day:10, label:'World Suicide Prevention Day',   type:'awareness' },
  { month:9,  day:12, label:'World First Aid Day',            type:'awareness' },
  { month:9,  day:15, label:'Int\'l Day of Democracy',        type:'awareness' },
  { month:9,  day:16, label:'Int\'l Day for Preservation of Ozone Layer', type:'awareness' },
  { month:9,  day:21, label:'Int\'l Day of Peace',            type:'awareness' },
  { month:9,  day:21, label:'World Alzheimer\'s Day',         type:'awareness' },
  { month:9,  day:26, label:'World Contraception Day',        type:'awareness' },
  { month:9,  day:27, label:'World Tourism Day',              type:'awareness' },
  { month:9,  day:28, label:'World Rabies Day',               type:'awareness' },
  { month:9,  day:29, label:'World Heart Day',                type:'awareness' },
  // October
  { month:10, day:1,  label:'Int\'l Day of Older Persons',    type:'awareness' },
  { month:10, day:2,  label:'Int\'l Day of Non-Violence',     type:'awareness' },
  { month:10, day:4,  label:'World Animal Day',               type:'awareness' },
  { month:10, day:5,  label:'World Teachers\' Day',           type:'awareness' },
  { month:10, day:9,  label:'World Post Day',                 type:'awareness' },
  { month:10, day:10, label:'World Mental Health Day',        type:'awareness' },
  { month:10, day:11, label:'Int\'l Day of the Girl Child',   type:'awareness' },
  { month:10, day:13, label:'Int\'l Day for Disaster Reduction', type:'awareness' },
  { month:10, day:14, label:'World Standards Day',            type:'awareness' },
  { month:10, day:15, label:'Int\'l Day of Rural Women',      type:'awareness' },
  { month:10, day:16, label:'World Food Day',                 type:'awareness' },
  { month:10, day:17, label:'Int\'l Day for Eradication of Poverty', type:'awareness' },
  { month:10, day:20, label:'World Statistics Day',           type:'awareness' },
  { month:10, day:24, label:'United Nations Day',             type:'awareness' },
  { month:10, day:31, label:'Halloween',                      type:'cultural'  },
  // November
  { month:11, day:6,  label:'Int\'l Day for Preventing Exploitation of Environment in War', type:'awareness' },
  { month:11, day:10, label:'World Science Day for Peace',    type:'awareness' },
  { month:11, day:11, label:'Remembrance Day',                type:'holiday'   },
  { month:11, day:14, label:'World Diabetes Day',             type:'awareness' },
  { month:11, day:16, label:'Int\'l Day for Tolerance',       type:'awareness' },
  { month:11, day:17, label:'Int\'l Students\' Day',          type:'awareness' },
  { month:11, day:19, label:'Int\'l Men\'s Day',              type:'awareness' },
  { month:11, day:19, label:'World Toilet Day',               type:'awareness' },
  { month:11, day:20, label:'Universal Children\'s Day',      type:'awareness' },
  { month:11, day:21, label:'World Television Day',           type:'awareness' },
  { month:11, day:25, label:'Int\'l Day for Elimination of Violence Against Women', type:'awareness' },
  { month:11, day:29, label:'Int\'l Day of Solidarity with Palestinian People', type:'awareness' },
  // December
  { month:12, day:1,  label:'World AIDS Day',                 type:'awareness' },
  { month:12, day:2,  label:'Int\'l Day for Abolition of Slavery', type:'awareness' },
  { month:12, day:3,  label:'Int\'l Day of Persons with Disabilities', type:'awareness' },
  { month:12, day:5,  label:'World Soil Day',                 type:'awareness' },
  { month:12, day:7,  label:'Int\'l Civil Aviation Day',      type:'awareness' },
  { month:12, day:9,  label:'Int\'l Anti-Corruption Day',     type:'awareness' },
  { month:12, day:10, label:'Human Rights Day',               type:'awareness' },
  { month:12, day:11, label:'Int\'l Mountain Day',            type:'awareness' },
  { month:12, day:18, label:'Int\'l Migrants Day',            type:'awareness' },
  { month:12, day:20, label:'Int\'l Human Solidarity Day',    type:'awareness' },
  { month:12, day:25, label:'Christmas Day',                  type:'holiday'   },
  { month:12, day:26, label:'Boxing Day',                     type:'holiday'   },
  { month:12, day:31, label:'New Year\'s Eve',                type:'cultural'  },
];

// ─── Easter (Western) — Anonymous Gregorian algorithm ────────────────────────
function easterDate(year: number): Date {
  const a = year % 19, b = Math.floor(year / 100), c = year % 100;
  const d = Math.floor(b / 4), e = b % 4, f = Math.floor((b + 8) / 25);
  const g = Math.floor((b - f + 1) / 3), h = (19*a + b - d - g + 15) % 30;
  const i = Math.floor(c / 4), k = c % 4;
  const l = (32 + 2*e + 2*i - h - k) % 7;
  const m = Math.floor((a + 11*h + 22*l) / 451);
  const month = Math.floor((h + l - 7*m + 114) / 31);
  const day   = ((h + l - 7*m + 114) % 31) + 1;
  return new Date(year, month - 1, day);
}

// ─── Holi — day after full moon in Phalguna (approx. March) ──────────────────
// Accurate lookup table for 2020-2035
const HOLI_DATES: Record<number, [number, number]> = {
  2020:[3,10], 2021:[3,29], 2022:[3,18], 2023:[3,8],  2024:[3,25],
  2025:[3,14], 2026:[3,3],  2027:[3,22], 2028:[3,11], 2029:[3,1],
  2030:[3,20], 2031:[3,9],  2032:[3,27], 2033:[3,16], 2034:[3,6],
  2035:[3,25],
};

// ─── Diwali — Amavasya of Kartik month (Oct/Nov) ─────────────────────────────
const DIWALI_DATES: Record<number, [number, number]> = {
  2020:[11,14], 2021:[11,4],  2022:[10,24], 2023:[11,12], 2024:[11,1],
  2025:[10,20], 2026:[11,8],  2027:[10,29], 2028:[10,17], 2029:[11,5],
  2030:[10,26], 2031:[11,14], 2032:[11,2],  2033:[10,22], 2034:[11,10],
  2035:[10,31],
};

// ─── Eid al-Fitr (end of Ramadan) ────────────────────────────────────────────
const EID_FITR_DATES: Record<number, [number, number]> = {
  2020:[5,24],  2021:[5,13],  2022:[5,2],   2023:[4,21],  2024:[4,10],
  2025:[3,30],  2026:[3,20],  2027:[3,9],   2028:[2,26],  2029:[2,14],
  2030:[2,4],   2031:[1,24],  2032:[1,14],  2033:[1,2],   2034:[12,23],
  2035:[12,12],
};

// ─── Eid al-Adha (Feast of Sacrifice) ────────────────────────────────────────
const EID_ADHA_DATES: Record<number, [number, number]> = {
  2020:[7,31],  2021:[7,20],  2022:[7,9],   2023:[6,28],  2024:[6,17],
  2025:[6,6],   2026:[5,27],  2027:[5,16],  2028:[5,5],   2029:[4,24],
  2030:[4,13],  2031:[4,2],   2032:[3,22],  2033:[3,11],  2034:[3,1],
  2035:[2,18],
};

// ─── Rosh Hashanah (Jewish New Year) ─────────────────────────────────────────
const ROSH_HASHANA_DATES: Record<number, [number, number]> = {
  2020:[9,19], 2021:[9,7],  2022:[9,26], 2023:[9,16], 2024:[10,3],
  2025:[9,23], 2026:[9,12], 2027:[10,2], 2028:[9,21], 2029:[9,10],
  2030:[9,28], 2031:[9,18], 2032:[9,6],  2033:[9,24], 2034:[9,14],
  2035:[10,4],
};

// ─── Yom Kippur ───────────────────────────────────────────────────────────────
const YOM_KIPPUR_DATES: Record<number, [number, number]> = {
  2020:[9,28], 2021:[9,16], 2022:[10,5], 2023:[9,25], 2024:[10,12],
  2025:[10,2], 2026:[9,21], 2027:[10,11],2028:[9,30], 2029:[9,19],
  2030:[10,7], 2031:[9,27], 2032:[9,15], 2033:[10,3], 2034:[9,23],
  2035:[10,13],
};

// ─── Vesak / Buddha Purnima ───────────────────────────────────────────────────
const VESAK_DATES: Record<number, [number, number]> = {
  2020:[5,7],  2021:[5,26], 2022:[5,16], 2023:[5,5],  2024:[5,23],
  2025:[5,12], 2026:[5,31], 2027:[5,21], 2028:[5,10], 2029:[4,28],
  2030:[5,18], 2031:[5,7],  2032:[5,25], 2033:[5,15], 2034:[5,4],
  2035:[5,23],
};

// ─── Guru Nanak Jayanti ───────────────────────────────────────────────────────
const GURU_NANAK_DATES: Record<number, [number, number]> = {
  2020:[11,30], 2021:[11,19], 2022:[11,8],  2023:[11,27], 2024:[11,15],
  2025:[11,5],  2026:[11,24], 2027:[11,13], 2028:[11,2],  2029:[11,21],
  2030:[11,10], 2031:[11,29], 2032:[11,18], 2033:[11,7],  2034:[11,26],
  2035:[11,16],
};

// ─── Navratri start (Shardiya) ────────────────────────────────────────────────
const NAVRATRI_DATES: Record<number, [number, number]> = {
  2020:[10,17], 2021:[10,7],  2022:[9,26],  2023:[10,15], 2024:[10,3],
  2025:[9,22],  2026:[10,11], 2027:[10,1],  2028:[10,19], 2029:[10,8],
  2030:[9,27],  2031:[10,16], 2032:[10,4],  2033:[9,24],  2034:[10,13],
  2035:[10,2],
};

// ─── Maha Shivaratri ──────────────────────────────────────────────────────────
const SHIVARATRI_DATES: Record<number, [number, number]> = {
  2020:[2,21], 2021:[3,11], 2022:[3,1],  2023:[2,18], 2024:[3,8],
  2025:[2,26], 2026:[2,15], 2027:[3,6],  2028:[2,23], 2029:[2,12],
  2030:[3,3],  2031:[2,20], 2032:[2,9],  2033:[2,27], 2034:[2,17],
  2035:[3,8],
};

// ─── Janmashtami ─────────────────────────────────────────────────────────────
const JANMASHTAMI_DATES: Record<number, [number, number]> = {
  2020:[8,12], 2021:[8,30], 2022:[8,19], 2023:[9,7],  2024:[8,26],
  2025:[8,16], 2026:[9,4],  2027:[8,24], 2028:[8,12], 2029:[9,1],
  2030:[8,21], 2031:[8,10], 2032:[8,28], 2033:[8,18], 2034:[8,7],
  2035:[8,27],
};

// ─── Onam (Thiruvonam) ────────────────────────────────────────────────────────
const ONAM_DATES: Record<number, [number, number]> = {
  2020:[8,31], 2021:[8,21], 2022:[9,8],  2023:[8,29], 2024:[9,15],
  2025:[9,5],  2026:[8,25], 2027:[9,13], 2028:[9,1],  2029:[8,22],
  2030:[9,10], 2031:[8,30], 2032:[9,17], 2033:[9,6],  2034:[8,27],
  2035:[9,15],
};

// ─── Pongal / Makar Sankranti (fixed Jan 14/15) ───────────────────────────────
// ─── Baisakhi (fixed Apr 13/14) ──────────────────────────────────────────────
// ─── Lohri (fixed Jan 13) ────────────────────────────────────────────────────

function lookup(table: Record<number,[number,number]>, year: number, label: string, type: SpecialDay['type']): SpecialDay | null {
  const entry = table[year];
  if (!entry) return null;
  return { month: entry[0], day: entry[1], label, type };
}

/** Returns all special days for a given year */
export function getSpecialDaysForYear(year: number): SpecialDay[] {
  const days: SpecialDay[] = [...FIXED_AWARENESS];

  // ── Easter & related ──────────────────────────────────────────────────────
  const easter = easterDate(year);
  const goodFriday = new Date(easter); goodFriday.setDate(easter.getDate() - 2);
  const easterMonday = new Date(easter); easterMonday.setDate(easter.getDate() + 1);
  days.push({ month: goodFriday.getMonth()+1,   day: goodFriday.getDate(),   label: 'Good Friday',   type: 'holiday'  });
  days.push({ month: easter.getMonth()+1,        day: easter.getDate(),       label: 'Easter Sunday', type: 'holiday'  });
  days.push({ month: easterMonday.getMonth()+1,  day: easterMonday.getDate(), label: 'Easter Monday', type: 'holiday'  });

  // ── Hindu ─────────────────────────────────────────────────────────────────
  const holi = lookup(HOLI_DATES, year, 'Holi 🎨', 'cultural');
  if (holi) days.push(holi);
  // Holika Dahan — day before Holi
  if (HOLI_DATES[year]) {
    const h = new Date(year, HOLI_DATES[year][0]-1, HOLI_DATES[year][1]-1);
    days.push({ month: h.getMonth()+1, day: h.getDate(), label: 'Holika Dahan 🔥', type: 'cultural' });
  }
  const diwali = lookup(DIWALI_DATES, year, 'Diwali 🪔', 'cultural');
  if (diwali) days.push(diwali);
  // Dhanteras — 2 days before Diwali
  if (DIWALI_DATES[year]) {
    const d = new Date(year, DIWALI_DATES[year][0]-1, DIWALI_DATES[year][1]-2);
    days.push({ month: d.getMonth()+1, day: d.getDate(), label: 'Dhanteras', type: 'cultural' });
  }
  const shivaratri = lookup(SHIVARATRI_DATES, year, 'Maha Shivaratri', 'cultural');
  if (shivaratri) days.push(shivaratri);
  const janmashtami = lookup(JANMASHTAMI_DATES, year, 'Janmashtami', 'cultural');
  if (janmashtami) days.push(janmashtami);
  const navratri = lookup(NAVRATRI_DATES, year, 'Navratri Begins', 'cultural');
  if (navratri) days.push(navratri);
  const onam = lookup(ONAM_DATES, year, 'Onam (Thiruvonam)', 'cultural');
  if (onam) days.push(onam);
  // Fixed Hindu
  days.push({ month:1,  day:13, label:'Lohri 🔥',              type:'cultural' });
  days.push({ month:1,  day:14, label:'Makar Sankranti / Pongal', type:'cultural' });
  days.push({ month:4,  day:13, label:'Baisakhi',               type:'cultural' });
  days.push({ month:4,  day:14, label:'Ambedkar Jayanti',        type:'cultural' });
  days.push({ month:10, day:2,  label:'Gandhi Jayanti',          type:'cultural' });

  // ── Islamic ───────────────────────────────────────────────────────────────
  const eidFitr = lookup(EID_FITR_DATES, year, 'Eid al-Fitr 🌙', 'cultural');
  if (eidFitr) days.push(eidFitr);
  const eidAdha = lookup(EID_ADHA_DATES, year, 'Eid al-Adha 🐑', 'cultural');
  if (eidAdha) days.push(eidAdha);

  // ── Jewish ────────────────────────────────────────────────────────────────
  const roshHashana = lookup(ROSH_HASHANA_DATES, year, 'Rosh Hashanah ✡️', 'cultural');
  if (roshHashana) days.push(roshHashana);
  const yomKippur = lookup(YOM_KIPPUR_DATES, year, 'Yom Kippur', 'cultural');
  if (yomKippur) days.push(yomKippur);

  // ── Buddhist ─────────────────────────────────────────────────────────────
  const vesak = lookup(VESAK_DATES, year, 'Vesak / Buddha Purnima ☸️', 'cultural');
  if (vesak) days.push(vesak);

  // ── Sikh ─────────────────────────────────────────────────────────────────
  const guruNanak = lookup(GURU_NANAK_DATES, year, 'Guru Nanak Jayanti', 'cultural');
  if (guruNanak) days.push(guruNanak);

  // ── Christian fixed ───────────────────────────────────────────────────────
  days.push({ month:12, day:24, label:'Christmas Eve',           type:'cultural' });

  return days;
}
