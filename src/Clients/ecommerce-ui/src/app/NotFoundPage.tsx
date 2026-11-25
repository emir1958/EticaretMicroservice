import { Compass } from "lucide-react";
import { ButtonLink } from "../shared/components/Button";
import EmptyState from "../shared/components/EmptyState";

export default function NotFoundPage() {
  return (
    <EmptyState
      icon={Compass}
      title="Aradığın sayfayı bulamadık"
      text="Bağlantı eski olabilir ya da adres yanlış yazılmış olabilir."
      action={
        <>
          <ButtonLink to="/">Ana sayfaya dön</ButtonLink>
          <ButtonLink to="/products" variant="secondary">
            Ürünlere göz at
          </ButtonLink>
        </>
      }
    />
  );
}
