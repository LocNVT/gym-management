import { Component, OnDestroy, ViewChild, ElementRef, Inject } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';

@Component({
    selector: 'app-camera-dialog',
    standalone: false,
    templateUrl: './camera-dialog.component.html',
    styleUrls: ['./camera-dialog.component.scss']
})
export class CameraDialogComponent implements OnDestroy {
    @ViewChild('videoElement') videoElement!: ElementRef<HTMLVideoElement>;
    @ViewChild('canvasElement') canvasElement!: ElementRef<HTMLCanvasElement>;

    stream: MediaStream | null = null;
    capturedImage: string | null = null;
    isCameraActive = false;
    errorMessage = '';

    constructor(
        public dialogRef: MatDialogRef<CameraDialogComponent>,
        @Inject(MAT_DIALOG_DATA) public data: { memberId: string, memberName: string }
    ) { }

    async startCamera(): Promise<void> {
        try {
            this.stream = await navigator.mediaDevices.getUserMedia({
                video: { width: 640, height: 480, facingMode: 'user' }
            });
            this.videoElement.nativeElement.srcObject = this.stream;
            this.isCameraActive = true;
            this.capturedImage = null;
            this.errorMessage = '';
        } catch (err) {
            this.errorMessage = 'Không thể mở camera. Vui lòng cho phép truy cập camera.';
        }
    }

    capture(): void {
        const video = this.videoElement.nativeElement;
        const canvas = this.canvasElement.nativeElement;
        canvas.width = video.videoWidth;
        canvas.height = video.videoHeight;
        const ctx = canvas.getContext('2d');
        if (ctx) {
            ctx.drawImage(video, 0, 0);
            this.capturedImage = canvas.toDataURL('image/jpeg', 0.8);
            this.stopCamera();
        }
    }

    retake(): void {
        this.capturedImage = null;
        this.startCamera();
    }

    save(): void {
        if (!this.capturedImage) return;
        const canvas = this.canvasElement.nativeElement;
        canvas.toBlob((blob) => {
            if (blob) {
                this.dialogRef.close(blob);
            }
        }, 'image/jpeg', 0.8);
    }

    stopCamera(): void {
        if (this.stream) {
            this.stream.getTracks().forEach(track => track.stop());
            this.stream = null;
            this.isCameraActive = false;
        }
    }

    cancel(): void {
        this.stopCamera();
        this.dialogRef.close(null);
    }

    ngOnDestroy(): void {
        this.stopCamera();
    }
}
