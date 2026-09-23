import { Component, Input } from '@angular/core';

@Component({
    selector: 'app-kpi-card',
    standalone: false,
    templateUrl: './kpi-card.component.html',
    styleUrls: ['./kpi-card.component.scss'],
})
export class KpiCardComponent {
    @Input({ required: true }) label!: string;
    @Input() value: number | null = null;
    @Input() previous: number | null = null;
    @Input() format: 'currency' | 'number' = 'number';
    @Input() icon = 'insights';
    /** Whether a rise in the value is good news. False for cost-type metrics (e.g. expense). */
    @Input() higherIsBetter = true;
    /**
     * Whether the month this card reports on is still in progress (e.g. it is 3 October and
     * this card is showing October). The card itself has no notion of "today" — the parent
     * knows which month it asked for and must say so explicitly.
     *
     * A handful of days of the current month compared against a full previous month is not a
     * real trend (three days of revenue reads as "-91% so với tháng trước" every month), so
     * while this is true the percentage is suppressed entirely rather than shown misleadingly.
     */
    @Input() isCurrentMonth = false;

    /** null means "no meaningful comparison", not "no change". */
    get changePercent(): number | null {
        if (this.isCurrentMonth) return null;
        if (this.value === null || this.previous === null || this.previous === 0) return null;
        return Math.round(((this.value - this.previous) / this.previous) * 100);
    }

    /**
     * True only for cards that would otherwise have shown a month-over-month comparison
     * (i.e. a `previous` was supplied) but are being suppressed because the month is still
     * in progress — this is what the neutral in-progress note replaces.
     */
    get showsInProgressNote(): boolean {
        return this.isCurrentMonth && this.previous !== null;
    }

    /** Which way the number actually moved — the arrow always follows this. */
    get direction(): 'up' | 'down' | 'flat' {
        const change = this.changePercent;
        if (change === null || change === 0) return 'flat';
        return change > 0 ? 'up' : 'down';
    }

    /** Whether the movement is good, bad, or neutral news — the colour follows this, not `direction`. */
    get sentiment(): 'good' | 'bad' | 'flat' {
        const change = this.changePercent;
        if (change === null || change === 0) return 'flat';
        const rose = change > 0;
        const isGood = this.higherIsBetter ? rose : !rose;
        return isGood ? 'good' : 'bad';
    }
}
