import { Component, OnInit } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import {
    MemberService,
    AttendanceDeviceService,
    FingerprintService
} from '../../../../shared/services/api.service';

/**
 * Combined fingerprint screen: enrol a member's finger, list/delete their templates,
 * and run a verify scan that toggles check-in / check-out.
 *
 * No physical scanner is wired in, so a "scan" is represented by a base64 template string.
 * For the Mock provider, enrolment derives a deterministic template from a seed phrase; entering
 * the same seed at verify time reproduces the template and matches, letting the whole flow be demoed.
 */
@Component({
    selector: 'app-fingerprint-attendance',
    standalone: false,
    templateUrl: './fingerprint-attendance.component.html',
    styleUrls: ['./fingerprint-attendance.component.scss']
})
export class FingerprintAttendanceComponent implements OnInit {
    members: any[] = [];
    devices: any[] = [];

    fingerPositions = Array.from({ length: 10 }, (_, i) => ({
        value: i,
        text: this.fingerLabel(i)
    }));

    // Enrolment state
    enrollMemberId: string | null = null;
    enrollFinger = 0;
    enrollSeed = '';
    memberTemplates: any[] = [];

    // Verify state
    verifyDeviceId: string | null = null;
    verifySeed = '';
    lastResult: any = null;

    constructor(
        private memberService: MemberService,
        private deviceService: AttendanceDeviceService,
        private fingerprintService: FingerprintService,
        private snackBar: MatSnackBar
    ) { }

    ngOnInit(): void {
        // Load a generous page of members/devices for the dropdowns.
        this.memberService.getAll(1, 200).subscribe(r => (this.members = r.items || []));
        this.deviceService.getAll(1, 200).subscribe(r => (this.devices = r.items || []));
    }

    onEnrollMemberChange(): void {
        this.loadMemberTemplates();
    }

    loadMemberTemplates(): void {
        if (!this.enrollMemberId) { this.memberTemplates = []; return; }
        this.fingerprintService.getByMember(this.enrollMemberId)
            .subscribe(list => (this.memberTemplates = list || []));
    }

    enroll(): void {
        if (!this.enrollMemberId) { this.notify('Vui lòng chọn thành viên'); return; }
        if (!this.enrollSeed) { this.notify('Vui lòng nhập mã vân tay (seed)'); return; }

        const payload = {
            memberId: this.enrollMemberId,
            fingerPosition: this.enrollFinger,
            capturedTemplate: this.seedToBase64(this.enrollSeed),
            vendor: 'Mock',
            quality: 90
        };

        this.fingerprintService.register(payload).subscribe({
            next: () => {
                this.notify('Đã đăng ký vân tay');
                this.enrollSeed = '';
                this.loadMemberTemplates();
            },
            error: () => this.notify('Lỗi khi đăng ký vân tay')
        });
    }

    deleteTemplate(id: string): void {
        this.fingerprintService.delete(id).subscribe({
            next: () => { this.notify('Đã xóa vân tay'); this.loadMemberTemplates(); },
            error: () => this.notify('Lỗi khi xóa vân tay')
        });
    }

    verify(): void {
        if (!this.verifyDeviceId) { this.notify('Vui lòng chọn thiết bị'); return; }
        if (!this.verifySeed) { this.notify('Vui lòng nhập mã vân tay (seed) để quét'); return; }

        const payload = {
            deviceId: this.verifyDeviceId,
            capturedTemplate: this.seedToBase64(this.verifySeed)
        };

        this.fingerprintService.verify(payload).subscribe({
            next: (res) => {
                this.lastResult = res;
                if (res.matched) {
                    const verb = res.action === 'check-in' ? 'Check-in' : 'Check-out';
                    this.notify(`${verb}: ${res.memberName} (điểm ${res.score})`);
                } else {
                    this.notify(`Không khớp (điểm cao nhất ${res.score})`);
                }
            },
            error: (err) => this.notify(err?.error?.message || 'Lỗi khi xác minh')
        });
    }

    fingerLabel(i: number): string {
        const labels = [
            'Ngón cái phải', 'Ngón trỏ phải', 'Ngón giữa phải', 'Ngón áp út phải', 'Ngón út phải',
            'Ngón cái trái', 'Ngón trỏ trái', 'Ngón giữa trái', 'Ngón áp út trái', 'Ngón út trái'
        ];
        return labels[i] ?? `Ngón ${i}`;
    }

    /**
     * Deterministically expand a seed phrase into a 32-byte base64 template.
     * Same seed -> same bytes, so a member enrolled with seed "X" matches a scan of seed "X".
     */
    private seedToBase64(seed: string): string {
        const bytes = new Uint8Array(32);
        for (let i = 0; i < 32; i++) {
            let h = 2166136261 ^ (i + 1);
            for (let j = 0; j < seed.length; j++) {
                h = Math.imul(h ^ seed.charCodeAt(j), 16777619);
            }
            bytes[i] = h & 0xff;
        }
        let binary = '';
        bytes.forEach(b => (binary += String.fromCharCode(b)));
        return btoa(binary);
    }

    private notify(message: string): void {
        this.snackBar.open(message, 'OK', { duration: 3000 });
    }
}
