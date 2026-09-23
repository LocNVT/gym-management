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

    /** null means "no meaningful comparison", not "no change". */
    get changePercent(): number | null {
        if (this.value === null || this.previous === null || this.previous === 0) return null;
        return Math.round(((this.value - this.previous) / this.previous) * 100);
    }

    get direction(): 'up' | 'down' | 'flat' {
        const change = this.changePercent;
        if (change === null || change === 0) return 'flat';
        return change > 0 ? 'up' : 'down';
    }
}
