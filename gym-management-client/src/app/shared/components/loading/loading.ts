import { Component } from '@angular/core';

@Component({
  selector: 'app-loading',
  standalone: false,
  template: `
        <div class="loading-container">
            <p>Loading...</p>
        </div>
    `,
  styles: [`.loading-container { display: flex; justify-content: center; align-items: center; height: 200px; }`]
})
export class LoadingComponent { }
