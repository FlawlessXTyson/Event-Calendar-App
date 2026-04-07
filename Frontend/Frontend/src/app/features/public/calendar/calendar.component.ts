import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { EventService } from '../../../core/services/event.service';
import { EventResponse } from '../../../core/models/models';
import { SpecialDay, getSpecialDaysForYear } from './special-days';

@Component({
  selector: 'app-calendar',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './calendar.component.html',
})
export class CalendarComponent implements OnInit {
  private eventSvc = inject(EventService);

  events   = signal<EventResponse[]>([]);
  loading  = signal(true);
  viewDate = signal(new Date());

  // Cache computed special days per year
  private specialDaysCache = new Map<number, SpecialDay[]>();

  days   = ['Sun','Mon','Tue','Wed','Thu','Fri','Sat'];
  legend = [
    { label: 'Public Holiday',    bg: '#FEE2E2' },
    { label: 'Cultural / Religious', bg: '#FEF3C7' },
    { label: 'Awareness Day',     bg: '#D1FAE5' },
    { label: 'Organizer Event',   bg: '#DBEAFE' },
  ];

  cells = computed(() => {
    const d   = this.viewDate();
    const yr  = d.getFullYear();
    const mo  = d.getMonth();       // 0-based
    const mo1 = mo + 1;             // 1-based
    const first       = new Date(yr, mo, 1).getDay();
    const daysInMonth = new Date(yr, mo + 1, 0).getDate();
    const today       = new Date();

    // Get (or compute + cache) special days for this year
    if (!this.specialDaysCache.has(yr)) {
      this.specialDaysCache.set(yr, getSpecialDaysForYear(yr));
    }
    const special = this.specialDaysCache.get(yr)!;

    type Cell = {
      key: string; day: number | null;
      isToday: boolean; isOther: boolean;
      events: EventResponse[];
      holidays: SpecialDay[];
      cultural: SpecialDay[];
      awareness: SpecialDay[];
    };

    const cells: Cell[] = [];

    for (let i = 0; i < first; i++)
      cells.push({ key:`p${i}`, day:null, isToday:false, isOther:true, events:[], holidays:[], cultural:[], awareness:[] });

    for (let day = 1; day <= daysInMonth; day++) {
      const evs      = this.events().filter(ev => {
        const ed = new Date(ev.eventDate);
        return ed.getFullYear()===yr && ed.getMonth()===mo && ed.getDate()===day;
      });
      const daySpecial = special.filter(s => s.month===mo1 && s.day===day);
      const holidays  = daySpecial.filter(s => s.type==='holiday');
      const cultural  = daySpecial.filter(s => s.type==='cultural');
      const awareness = daySpecial.filter(s => s.type==='awareness');
      const isToday   = today.getFullYear()===yr && today.getMonth()===mo && today.getDate()===day;

      cells.push({ key:`${yr}-${mo}-${day}`, day, isToday, isOther:false, events:evs, holidays, cultural, awareness });
    }

    const rem = cells.length % 7;
    if (rem > 0)
      for (let i = 0; i < 7-rem; i++)
        cells.push({ key:`n${i}`, day:null, isToday:false, isOther:true, events:[], holidays:[], cultural:[], awareness:[] });

    return cells;
  });

  ngOnInit() {
    this.eventSvc.getAll().subscribe({
      next: evs => { this.events.set(evs); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  prevMonth() {
    const d = this.viewDate();
    this.viewDate.set(new Date(d.getFullYear(), d.getMonth()-1, 1));
  }

  nextMonth() {
    const d = this.viewDate();
    this.viewDate.set(new Date(d.getFullYear(), d.getMonth()+1, 1));
  }

  cellStyle(c: any): string {
    if (c.isOther) return 'background:var(--surface-2);opacity:.5;';
    if (c.isToday) return 'background:#EEF2FF;';
    return 'background:var(--surface);';
  }

  dayNumStyle(c: any): string {
    return c.isToday ? 'background:var(--primary);color:#fff;' : '';
  }

  evColor(cat: number) {
    const map: Record<number,{bg:string;text:string}> = {
      1:{bg:'#FEE2E2',text:'#991B1B'},
      2:{bg:'#D1FAE5',text:'#065F46'},
      3:{bg:'#DBEAFE',text:'#1E40AF'},
      4:{bg:'#EDE9FE',text:'#5B21B6'},
    };
    return map[cat] ?? {bg:'#F1F5F9',text:'#475569'};
  }
}
