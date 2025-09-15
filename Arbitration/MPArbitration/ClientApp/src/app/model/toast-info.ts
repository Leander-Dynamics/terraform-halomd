import { ToastEnum } from "./toast-enum";

export interface ToastInfo {
    body: string;
    delay?: number;
    header: string;
    class?: string;
  }