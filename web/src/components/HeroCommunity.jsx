import { Icon } from "./UI";
export default function HeroCommunity() {
  return (
    <div className="p-hero-visual">
      <div className="p-hero-image-accent" aria-hidden="true" />
      <picture>
        <source
          media="(max-width: 599px)"
          srcSet="/assets/hero-community-sm.jpg"
          width="512"
          height="768"
        />
        <img
          src="/assets/hero-community.jpg"
          srcSet="/assets/hero-community-sm.jpg 512w, /assets/hero-community.jpg 1024w"
          sizes="(min-width: 900px) 44vw, (min-width: 600px) 560px, calc(100vw - 48px)"
          width="1024"
          height="1536"
          fetchPriority="high"
          decoding="async"
          alt="CrowdAid volunteers in white vests collecting donations and sharing flyers at a local market"
        />
      </picture>
      <div className="p-hero-floating p-hero-reviewed">
        <span className="p-hero-check" aria-hidden="true">
          ✓
        </span>
        <div>
          <strong>Reviewed requests</strong>
          <small>Care behind every campaign</small>
        </div>
      </div>
      <div className="p-hero-floating p-hero-ways">
        <Icon name="community" />
        <span>Donations · Practical help · Volunteers</span>
      </div>
    </div>
  );
}
